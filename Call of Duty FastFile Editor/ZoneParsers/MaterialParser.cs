using Call_of_Duty_FastFile_Editor.Models;
using System.Diagnostics;
using System.Text;

namespace Call_of_Duty_FastFile_Editor.ZoneParsers
{
    /// <summary>
    /// Parser for Material assets in zone files.
    ///
    /// Material structure (CoD4/WaW/MW2/MW3):
    /// - MaterialInfo info (0x20 bytes, starts with name pointer)
    /// - char stateBitsEntry[TECHNIQUE_COUNT]
    /// - char textureCount, constantCount, stateBitsCount, stateFlags, cameraRegion
    /// - MaterialTechniqueSet *techniqueSet
    /// - MaterialTextureDef *textureTable
    /// - MaterialConstantDef *constantTable
    /// - GfxStateBits *stateBitTable
    ///
    /// MaterialInfo structure (0x20 bytes):
    /// - const char *name (pointer, FF FF FF FF in zone)
    /// - game flags, sort key, atlas dimensions, draw surface, surface type bits
    /// </summary>
    public static class MaterialParser
    {
        // MaterialInfo size
        private const int MATERIAL_INFO_SIZE = 0x20;

        /// <summary>
        /// Parses a Material asset from zone data.
        /// </summary>
        /// <param name="zoneData">The zone file data.</param>
        /// <param name="offset">Starting offset to parse from.</param>
        /// <param name="isBigEndian">Whether data is big-endian (PS3/Xbox).</param>
        /// <returns>Parsed MaterialAsset, or null if parsing failed.</returns>
        public static MaterialAsset? ParseMaterial(byte[] zoneData, int offset, bool isBigEndian = true)
        {
            Debug.WriteLine($"[MaterialParser] Parsing at offset 0x{offset:X}");

            // Need at least MaterialInfo size (0x20) plus some more for counts
            if (offset + MATERIAL_INFO_SIZE + 8 > zoneData.Length)
            {
                Debug.WriteLine($"[MaterialParser] Not enough data at 0x{offset:X}");
                return null;
            }

            // MaterialInfo starts with name pointer (FF FF FF FF)
            if (zoneData[offset] != 0xFF || zoneData[offset + 1] != 0xFF ||
                zoneData[offset + 2] != 0xFF || zoneData[offset + 3] != 0xFF)
            {
                // Log actual bytes found for debugging
                string actualBytes = $"{zoneData[offset]:X2} {zoneData[offset + 1]:X2} {zoneData[offset + 2]:X2} {zoneData[offset + 3]:X2}";
                Debug.WriteLine($"[MaterialParser] No name pointer marker at 0x{offset:X}. Found: {actualBytes} (expected FF FF FF FF)");
                return null;
            }

            // After MaterialInfo (0x20 bytes), we have stateBitsEntry array
            // The size of stateBitsEntry depends on TECHNIQUE_COUNT which varies by platform
            // For PS3 WaW, it's typically around 48-54 techniques
            // After that are the counts: textureCount, constantCount, stateBitsCount, stateFlags, cameraRegion

            // For now, scan forward to find the name string
            // The name follows the header structure
            int nameSearchStart = offset + MATERIAL_INFO_SIZE;
            int nameOffset = -1;

            // Look for a valid material name
            for (int i = nameSearchStart; i < offset + 256 && i < zoneData.Length - 1; i++)
            {
                byte b = zoneData[i];
                if (b >= 0x20 && b <= 0x7E && b != 0xFF)
                {
                    string potentialName = ReadNullTerminatedString(zoneData, i);

                    // Material names often start with "," or are paths
                    if (potentialName.Length >= 2 && potentialName.Length <= 128 &&
                        (potentialName.StartsWith(",") || potentialName.StartsWith("mc/") ||
                         potentialName.StartsWith("menu_") || potentialName.StartsWith("gfx_") ||
                         potentialName.Contains("/") || IsValidMaterialName(potentialName)))
                    {
                        nameOffset = i;
                        break;
                    }
                }
            }

            if (nameOffset == -1)
            {
                Debug.WriteLine($"[MaterialParser] Could not find material name at 0x{offset:X}");
                return null;
            }

            string name = ReadNullTerminatedString(zoneData, nameOffset);
            int nameEndOffset = nameOffset + Encoding.ASCII.GetByteCount(name) + 1;

            // Try to extract texture count from known offset in MaterialInfo
            // textureCount is at offset 0x20 + stateBitsEntry length + 0
            // For simplicity, we'll just record the name for now

            var asset = new MaterialAsset
            {
                Name = name,
                StartOfFileHeader = offset,
                EndOffset = nameEndOffset,
                AdditionalData = $"Parsed from offset 0x{offset:X}"
            };

            Debug.WriteLine($"[MaterialParser] Found material: '{name}'");
            return asset;
        }

        /// <summary>
        /// Scans for the next material asset after a given offset.
        /// </summary>
        public static MaterialAsset? FindNextMaterial(byte[] zoneData, int startOffset)
        {
            for (int i = startOffset; i < zoneData.Length - MATERIAL_INFO_SIZE; i++)
            {
                var result = ParseMaterial(zoneData, i);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private static bool IsValidMaterialName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            // Material names are typically alphanumeric with underscores, slashes, dots
            // Some start with "," for menu materials
            foreach (char c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '.' && c != '/' &&
                    c != '\\' && c != '-' && c != ',' && c != '$')
                {
                    return false;
                }
            }
            return true;
        }

        private static string ReadNullTerminatedString(byte[] data, int offset)
        {
            var sb = new StringBuilder();
            while (offset < data.Length && data[offset] != 0x00)
            {
                sb.Append((char)data[offset]);
                offset++;
            }
            return sb.ToString();
        }

        private static int ReadInt32BE(byte[] data, int offset)
        {
            return (data[offset] << 24) | (data[offset + 1] << 16) |
                   (data[offset + 2] << 8) | data[offset + 3];
        }
    }
}
