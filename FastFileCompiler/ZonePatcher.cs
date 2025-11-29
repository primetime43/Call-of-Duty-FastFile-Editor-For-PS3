using System.Text;
using FastFileCompiler.Models;

namespace FastFileCompiler;

/// <summary>
/// Result of a zone patching operation.
/// </summary>
public class PatchResult
{
    public byte[] PatchedZone { get; set; } = Array.Empty<byte>();
    public List<string> ReplacedFiles { get; set; } = new();
    public List<string> SkippedFiles { get; set; } = new();
    public bool HasSkippedFiles => SkippedFiles.Count > 0;
}

/// <summary>
/// Patches an existing zone file by replacing raw files while preserving all other zone structure.
/// NOTE: Only REPLACEMENTS are supported. Adding new rawfiles is not possible due to zone structure constraints.
/// </summary>
public class ZonePatcher
{
    private readonly byte[] _originalZone;
    private readonly GameVersion _gameVersion;

    private static readonly string[] ValidExtensions = {
        ".cfg", ".gsc", ".atr", ".csc", ".rmb", ".arena", ".vision", ".txt", ".str", ".menu"
    };

    public ZonePatcher(byte[] originalZone, GameVersion gameVersion)
    {
        _originalZone = originalZone;
        _gameVersion = gameVersion;
    }

    /// <summary>
    /// Patches the zone by replacing existing raw files.
    /// Files that don't exist in the zone will be skipped (additions not supported).
    /// </summary>
    public PatchResult PatchWithResult(List<RawFile> files)
    {
        var result = new PatchResult();
        result.PatchedZone = PatchInternal(files, result.ReplacedFiles, result.SkippedFiles);
        return result;
    }

    /// <summary>
    /// Patches the zone by replacing existing raw files.
    /// Preserves all other zone structure (asset pool, localize entries, footer, etc.)
    /// </summary>
    public byte[] Patch(List<RawFile> files)
    {
        return PatchInternal(files, null, null);
    }

    private byte[] PatchInternal(List<RawFile> files, List<string>? replacedFiles, List<string>? skippedFiles)
    {
        // Find all existing raw files in the zone
        var existingFiles = FindRawFilesInZone(_originalZone);

        // Separate files into replacements vs additions
        var replacements = new List<(RawFile file, RawFileLocation loc)>();
        var additions = new List<RawFile>();

        foreach (var file in files)
        {
            var existing = existingFiles.FirstOrDefault(f =>
                f.Name.Equals(file.Name, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
                replacements.Add((file, existing));
            else
                additions.Add(file);
        }

        // Work with a copy of the zone
        var zone = new List<byte>(_originalZone);
        int totalShift = 0;

        // Sort replacements by offset
        replacements.Sort((a, b) => a.loc.DataOffset.CompareTo(b.loc.DataOffset));

        // Step 1: Replace existing files
        foreach (var (file, loc) in replacements)
        {
            int adjSizeOffset = loc.SizeOffset + totalShift;
            int adjDataOffset = loc.DataOffset + totalShift;

            int oldSize = loc.DataSize;
            int newSize = file.Data.Length;
            int sizeDiff = newSize - oldSize;

            // Update size field (big-endian)
            zone[adjSizeOffset] = (byte)(newSize >> 24);
            zone[adjSizeOffset + 1] = (byte)(newSize >> 16);
            zone[adjSizeOffset + 2] = (byte)(newSize >> 8);
            zone[adjSizeOffset + 3] = (byte)newSize;

            if (sizeDiff == 0)
            {
                for (int i = 0; i < newSize; i++)
                    zone[adjDataOffset + i] = file.Data[i];
            }
            else if (sizeDiff < 0)
            {
                for (int i = 0; i < newSize; i++)
                    zone[adjDataOffset + i] = file.Data[i];
                zone.RemoveRange(adjDataOffset + newSize, -sizeDiff);
                totalShift += sizeDiff;
            }
            else
            {
                for (int i = 0; i < oldSize; i++)
                    zone[adjDataOffset + i] = file.Data[i];
                var extra = new byte[sizeDiff];
                Array.Copy(file.Data, oldSize, extra, 0, sizeDiff);
                zone.InsertRange(adjDataOffset + oldSize, extra);
                totalShift += sizeDiff;
            }

            // Track replaced file
            replacedFiles?.Add(file.Name);
        }

        // Step 2: Handle additions (new files that don't exist in zone)
        if (additions.Count > 0)
        {
            // LIMITATION: Adding new rawfiles to existing zones is NOT supported.
            //
            // Technical reason: The game requires pool entries to find rawfiles.
            // Adding pool entries requires inserting bytes in the middle of the zone,
            // which shifts all asset data and corrupts internal zone references.
            //
            // Workaround: Create a separate FF with just the new rawfiles using the compiler.
            //
            // Track skipped files for reporting
            foreach (var file in additions)
            {
                skippedFiles?.Add(file.Name);
            }
        }

        // Step 3: Update header sizes
        byte[] result = zone.ToArray();
        UpdateZoneHeaderSizes(result);

        return result;
    }

    private byte GetRawFileAssetType()
    {
        return _gameVersion switch
        {
            GameVersion.CoD4 => 0x21,
            GameVersion.WaW => 0x22,
            GameVersion.MW2 => 0x23,
            _ => 0x21
        };
    }

    /// <summary>
    /// Builds raw file data bytes: FF FF FF FF [size BE] FF FF FF FF [name\0] [data]
    /// </summary>
    private List<byte> BuildRawFileData(RawFile file)
    {
        var data = new List<byte>();
        data.AddRange(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });

        int size = file.Data.Length;
        data.Add((byte)(size >> 24));
        data.Add((byte)(size >> 16));
        data.Add((byte)(size >> 8));
        data.Add((byte)size);

        data.AddRange(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
        data.AddRange(Encoding.ASCII.GetBytes(file.Name));
        data.Add(0x00);
        data.AddRange(file.Data);
        data.Add(0x00); // Trailing null terminator (matches ZoneBuilder format)

        return data;
    }

    /// <summary>
    /// Finds the start and end of the asset pool section.
    /// Asset pool entries are 8 bytes: 00 00 00 [type] FF FF FF FF
    /// Pool terminates with: FF FF FF FF FF FF FF FF
    /// </summary>
    private (int start, int end) FindAssetPoolBounds(byte[] zoneData)
    {
        // Zone structure: Header (52 bytes) -> Script strings -> Asset pool -> Pool terminator -> Asset data
        // We need to skip past script strings first
        int scriptStringCount = (zoneData[36] << 24) | (zoneData[37] << 16) |
                                (zoneData[38] << 8) | zoneData[39];

        // Skip past script strings (null-terminated strings starting at offset 52)
        int pos = 52;
        for (int i = 0; i < scriptStringCount && pos < zoneData.Length; i++)
        {
            while (pos < zoneData.Length && zoneData[pos] != 0)
                pos++;
            pos++; // Skip null terminator
        }

        // Now pos should be at the start of the asset pool
        // Read the asset count from header to know how many entries to expect
        int assetCount = (zoneData[44] << 24) | (zoneData[45] << 16) |
                        (zoneData[46] << 8) | zoneData[47];

        // Verify this looks like the asset pool start
        if (pos + 8 <= zoneData.Length)
        {
            bool isPoolEntry = zoneData[pos] == 0x00 && zoneData[pos + 1] == 0x00 && zoneData[pos + 2] == 0x00 &&
                               zoneData[pos + 4] == 0xFF && zoneData[pos + 5] == 0xFF &&
                               zoneData[pos + 6] == 0xFF && zoneData[pos + 7] == 0xFF;
            if (isPoolEntry)
            {
                // Found the pool, return start and end (end = start + assetCount * 8)
                return (pos, pos + assetCount * 8);
            }
        }

        // Fallback: Search for longest consecutive run of pool entries
        int bestStart = -1;
        int bestCount = 0;
        int currentStart = -1;
        int currentCount = 0;

        for (int i = pos; i < zoneData.Length - 8; i++)
        {
            bool isPoolEntry = zoneData[i] == 0x00 && zoneData[i + 1] == 0x00 && zoneData[i + 2] == 0x00 &&
                               zoneData[i + 4] == 0xFF && zoneData[i + 5] == 0xFF &&
                               zoneData[i + 6] == 0xFF && zoneData[i + 7] == 0xFF;

            if (isPoolEntry)
            {
                if (currentStart < 0)
                {
                    currentStart = i;
                    currentCount = 1;
                }
                else if (i == currentStart + currentCount * 8)
                {
                    currentCount++;
                }
                else
                {
                    if (currentCount > bestCount)
                    {
                        bestCount = currentCount;
                        bestStart = currentStart;
                    }
                    currentStart = i;
                    currentCount = 1;
                }
            }
        }

        if (currentCount > bestCount)
        {
            bestCount = currentCount;
            bestStart = currentStart;
        }

        if (bestStart < 0 || bestCount < 10)
            return (-1, -1);

        return (bestStart, bestStart + bestCount * 8);
    }

    /// <summary>
    /// Finds the position to insert new raw file data (after last existing raw file).
    /// </summary>
    private int FindInsertPosition(List<byte> zone, List<RawFileLocation> existingFiles, int shift)
    {
        // First try to find the true last rawfile using pattern matching
        // This catches files without standard extensions like "info/bullet_penetration_mp"
        int lastRawfileEnd = FindLastRawfileEnd(_originalZone);
        if (lastRawfileEnd > 0)
        {
            return lastRawfileEnd + shift;
        }

        if (existingFiles.Count == 0)
        {
            // No existing raw files - insert right after asset pool (including terminator)
            var (_, poolEnd) = FindAssetPoolBounds(_originalZone);
            if (poolEnd > 0)
            {
                // Skip past the pool terminator (FF FF FF FF FF FF FF FF)
                return poolEnd + 8 + shift;
            }
            // Fallback: estimate based on asset count
            int assetCount = (_originalZone[44] << 24) | (_originalZone[45] << 16) |
                            (_originalZone[46] << 8) | _originalZone[47];
            return 52 + (assetCount * 8) + shift;
        }

        // Fallback: Find the last raw file by data offset from detected files
        var lastFile = existingFiles.OrderByDescending(f => f.DataOffset).First();
        return lastFile.DataOffset + lastFile.DataSize + shift;
    }

    /// <summary>
    /// Finds the end position of the last rawfile in the zone by scanning for
    /// rawfile header patterns: FF FF FF FF [size] FF FF FF FF [name\0] [data]
    /// This catches all rawfiles regardless of file extension.
    /// </summary>
    private int FindLastRawfileEnd(byte[] zoneData)
    {
        int lastEnd = -1;

        // Start after header and asset pool
        int assetCount = (zoneData[44] << 24) | (zoneData[45] << 16) |
                        (zoneData[46] << 8) | zoneData[47];
        var (poolStart, poolEnd) = FindAssetPoolBounds(zoneData);
        int searchStart = poolEnd > 0 ? poolEnd : 52 + assetCount * 8;

        for (int i = searchStart; i < zoneData.Length - 20; i++)
        {
            // Look for rawfile header pattern: FF FF FF FF [4-byte size] FF FF FF FF
            if (zoneData[i] == 0xFF && zoneData[i + 1] == 0xFF &&
                zoneData[i + 2] == 0xFF && zoneData[i + 3] == 0xFF &&
                zoneData[i + 8] == 0xFF && zoneData[i + 9] == 0xFF &&
                zoneData[i + 10] == 0xFF && zoneData[i + 11] == 0xFF)
            {
                // Read size (big-endian)
                int size = (zoneData[i + 4] << 24) | (zoneData[i + 5] << 16) |
                          (zoneData[i + 6] << 8) | zoneData[i + 7];

                // Sanity check on size
                if (size <= 0 || size > 10_000_000) continue;

                // Find name (null-terminated string after second FF marker)
                int nameStart = i + 12;
                int nameEnd = nameStart;
                while (nameEnd < zoneData.Length && zoneData[nameEnd] != 0 && nameEnd - nameStart < 300)
                    nameEnd++;

                if (nameEnd <= nameStart) continue;

                // Validate name looks like a path (starts with letter or special chars)
                char firstChar = (char)zoneData[nameStart];
                if (!char.IsLetter(firstChar) && firstChar != '_' && firstChar != '/') continue;

                // Calculate data end position
                int dataStart = nameEnd + 1;
                int dataEnd = dataStart + size;

                if (dataEnd <= zoneData.Length && dataEnd > lastEnd)
                {
                    lastEnd = dataEnd;
                }
            }
        }

        return lastEnd;
    }

    private List<RawFileLocation> FindRawFilesInZone(byte[] zoneData)
    {
        var files = new List<RawFileLocation>();
        var foundOffsets = new HashSet<int>();

        foreach (var ext in ValidExtensions)
        {
            byte[] pattern = Encoding.ASCII.GetBytes(ext + "\0");

            for (int i = 0; i <= zoneData.Length - pattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (zoneData[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (!match) continue;

                int markerEnd = i - 1;
                while (markerEnd >= 4)
                {
                    if (zoneData[markerEnd] == 0xFF &&
                        zoneData[markerEnd - 1] == 0xFF &&
                        zoneData[markerEnd - 2] == 0xFF &&
                        zoneData[markerEnd - 3] == 0xFF)
                        break;
                    markerEnd--;
                    if (i - markerEnd > 300)
                    {
                        markerEnd = -1;
                        break;
                    }
                }

                if (markerEnd < 4) continue;
                if (zoneData[markerEnd + 1] == 0x00) continue;

                int sizeOffset = markerEnd - 7;
                if (sizeOffset < 0) continue;

                int headerOffset = sizeOffset - 4;
                if (headerOffset < 0) continue;
                if (foundOffsets.Contains(headerOffset)) continue;

                int size = (zoneData[sizeOffset] << 24) |
                          (zoneData[sizeOffset + 1] << 16) |
                          (zoneData[sizeOffset + 2] << 8) |
                          zoneData[sizeOffset + 3];

                if (size <= 0 || size > 10_000_000) continue;

                int nameStart = markerEnd + 1;
                int nameEnd = nameStart;
                while (nameEnd < zoneData.Length && zoneData[nameEnd] != 0)
                    nameEnd++;

                if (nameEnd <= nameStart) continue;

                string name = Encoding.ASCII.GetString(zoneData, nameStart, nameEnd - nameStart);
                if (!name.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) continue;

                int dataOffset = nameEnd + 1;

                files.Add(new RawFileLocation
                {
                    Name = name,
                    HeaderOffset = headerOffset,
                    SizeOffset = sizeOffset,
                    NameOffset = nameStart,
                    DataOffset = dataOffset,
                    DataSize = size
                });

                foundOffsets.Add(headerOffset);
            }
        }

        return files;
    }

    private void UpdateZoneHeaderSizes(byte[] zoneData)
    {
        // Zone header structure (from CoD Research wiki):
        // 0x00: ZoneSize - total size of zone data excluding 36-byte XFile header
        // 0x18 (24): BlockSizeLarge - DO NOT MODIFY (memory allocation)
        // 0x2C (44): AssetCount - updated separately when adding assets

        // Calculate ZoneSize: total length minus 36-byte XFile header
        // The XFile header is 36 bytes, XAssetList is 16 bytes = 52 total header
        // But ZoneSize field counts from after XFile header (36 bytes)
        int zoneSize = zoneData.Length - 36;

        // Update only offset 0x00: ZoneSize
        zoneData[0] = (byte)(zoneSize >> 24);
        zoneData[1] = (byte)(zoneSize >> 16);
        zoneData[2] = (byte)(zoneSize >> 8);
        zoneData[3] = (byte)zoneSize;
    }

    private class RawFileLocation
    {
        public string Name { get; set; } = "";
        public int HeaderOffset { get; set; }
        public int SizeOffset { get; set; }
        public int NameOffset { get; set; }
        public int DataOffset { get; set; }
        public int DataSize { get; set; }
    }
}
