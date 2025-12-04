using Call_of_Duty_FastFile_Editor.Models;
using System.Diagnostics;
using System.Text;

namespace Call_of_Duty_FastFile_Editor.ZoneParsers
{
    /// <summary>
    /// Parser for TechniqueSet assets in zone files.
    /// TechniqueSet structure:
    /// [name_ptr (FF FF FF FF)] [worldVertFormat 4 bytes] [technique ptrs...]
    /// Followed by null-terminated name string.
    /// </summary>
    public static class TechSetParser
    {
        /// <summary>
        /// Parses a TechniqueSet asset from zone data.
        /// </summary>
        /// <param name="zoneData">The zone file data.</param>
        /// <param name="offset">Starting offset to parse from.</param>
        /// <param name="isBigEndian">Whether data is big-endian (PS3/Xbox).</param>
        /// <returns>Parsed TechSetAsset, or null if parsing failed.</returns>
        public static TechSetAsset? ParseTechSet(byte[] zoneData, int offset, bool isBigEndian = true)
        {
            Debug.WriteLine($"[TechSetParser] Parsing at offset 0x{offset:X}");

            // Need at least 8 bytes for minimal header
            if (offset + 8 > zoneData.Length)
            {
                Debug.WriteLine($"[TechSetParser] Not enough data at 0x{offset:X}");
                return null;
            }

            // Check for name pointer marker (FF FF FF FF)
            if (zoneData[offset] != 0xFF || zoneData[offset + 1] != 0xFF ||
                zoneData[offset + 2] != 0xFF || zoneData[offset + 3] != 0xFF)
            {
                // Log actual bytes found for debugging
                string actualBytes = $"{zoneData[offset]:X2} {zoneData[offset + 1]:X2} {zoneData[offset + 2]:X2} {zoneData[offset + 3]:X2}";
                Debug.WriteLine($"[TechSetParser] No pointer marker at 0x{offset:X}. Found: {actualBytes} (expected FF FF FF FF)");
                return null;
            }

            // Read worldVertFormat (4 bytes after name pointer)
            int worldVertFormat = isBigEndian
                ? ReadInt32BE(zoneData, offset + 4)
                : ReadInt32LE(zoneData, offset + 4);

            // Validate worldVertFormat (should be 0x0 to 0x0B based on docs)
            if (worldVertFormat < 0 || worldVertFormat > 0x0B)
            {
                // Could still be valid with different format values
                Debug.WriteLine($"[TechSetParser] Unusual worldVertFormat: 0x{worldVertFormat:X}");
            }

            // After header, look for the name string
            // The name follows the technique pointers array
            // For simplicity, scan forward for a valid string
            int nameOffset = offset + 8;

            // Skip any additional pointer markers and find the name
            while (nameOffset < zoneData.Length - 1 && nameOffset < offset + 256)
            {
                // Check if this looks like a valid ASCII string start
                byte b = zoneData[nameOffset];
                if (b >= 0x20 && b <= 0x7E && b != 0xFF)
                {
                    // Might be the start of the name
                    string potentialName = ReadNullTerminatedString(zoneData, nameOffset);

                    // Validate it looks like a techset name (should be relatively short, no weird chars)
                    if (potentialName.Length >= 3 && potentialName.Length <= 64 &&
                        IsValidAssetName(potentialName))
                    {
                        int nameEndOffset = nameOffset + Encoding.ASCII.GetByteCount(potentialName) + 1;

                        var asset = new TechSetAsset
                        {
                            Name = potentialName,
                            WorldVertFormat = worldVertFormat,
                            StartOfFileHeader = offset,
                            EndOffset = nameEndOffset,
                            AdditionalData = $"Parsed from offset 0x{offset:X}"
                        };

                        Debug.WriteLine($"[TechSetParser] Found techset: '{potentialName}'");
                        return asset;
                    }
                }
                nameOffset++;
            }

            Debug.WriteLine($"[TechSetParser] Could not find name string at 0x{offset:X}");
            return null;
        }

        /// <summary>
        /// Scans for techset assets using pattern matching.
        /// </summary>
        public static TechSetAsset? FindNextTechSet(byte[] zoneData, int startOffset)
        {
            for (int i = startOffset; i < zoneData.Length - 12; i++)
            {
                var result = ParseTechSet(zoneData, i);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private static bool IsValidAssetName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            // Asset names typically contain alphanumeric, underscores, dots, slashes
            foreach (char c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '.' && c != '/' && c != '\\' && c != '-')
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

        private static int ReadInt32LE(byte[] data, int offset)
        {
            return data[offset] | (data[offset + 1] << 8) |
                   (data[offset + 2] << 16) | (data[offset + 3] << 24);
        }
    }
}
