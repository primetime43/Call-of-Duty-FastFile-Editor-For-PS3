using Call_of_Duty_FastFile_Editor.Services; // for Utilities.ReadStringAtOffset

namespace Call_of_Duty_FastFile_Editor.Models
{
    public class TagCollection
    {
        public List<ZoneAsset_TagEntry> TagEntries { get; set; }
        = new List<ZoneAsset_TagEntry>();
    }

    public class ZoneAsset_TagEntry
    {
        public string Tag { get; set; }
        public int OffsetDec { get; set; }
        public string OffsetHex { get; set; }
    }

    // Eventually rewrite this to use the asset pool start offset to get the tags
    public static class TagOperations
    {
        /// <summary>
        /// Finds a large run (16 bytes) of 0xFF in the zone’s byte array.
        /// Returns the start offset of that run, or -1 if not found.
        /// Adjust if your format uses more or fewer 0xFF.
        /// </summary>
        private static int FindLargeFFBlock(byte[] zoneBytes)
        {
            const int runLength = 16;
            for (int i = 0; i <= zoneBytes.Length - runLength; i++)
            {
                bool foundRun = true;
                for (int j = 0; j < runLength; j++)
                {
                    if (zoneBytes[i + j] != 0xFF)
                    {
                        foundRun = false;
                        break;
                    }
                }
                if (foundRun) return i;
            }
            return -1;
        }

        /// <summary>
        /// Reads tags appearing after a large run of 0xFF, ending at the first
        /// occurrence of four consecutive 0x00 bytes (or out of file). Each tag 
        /// is null-terminated ASCII, and we store each tag's offset in both 
        /// decimal and hex.
        /// </summary>
        public static TagCollection? FindTags(ZoneFile zone)
        {
            byte[] zoneBytes = zone.Data;

            // 1) Locate the block of 0xFFs
            int blockStart = FindLargeFFBlock(zoneBytes);
            if (blockStart < 0)
            {
                // No large run of FF found
                return null;
            }

            // 2) Move offset to first NON-FF byte
            int offset = blockStart;
            while (offset < zoneBytes.Length && zoneBytes[offset] == 0xFF)
            {
                offset++;
            }

            if (offset >= zoneBytes.Length)
            {
                // We reached the end, no tags to parse
                return null;
            }

            // We'll collect TagEntry objects here
            List<ZoneAsset_TagEntry> tagEntries = new List<ZoneAsset_TagEntry>();

            // 3) Read strings until 00 00 00 or out of data
            while (offset + 3 < zoneBytes.Length)
            {
                if (zoneBytes[offset] == 0x00 &&
                    zoneBytes[offset + 1] == 0x00 &&
                    zoneBytes[offset + 2] == 0x00)
                {
                    break;
                }

                // Store the offset (this is the *start* of the string)
                int currentOffset = offset;

                // Read a null-terminated ASCII string
                string tag = Utilities.ReadStringAtOffset(offset, zone);

                // If valid, add it
                if (!string.IsNullOrEmpty(tag))
                {
                    var entry = new ZoneAsset_TagEntry
                    {
                        Tag = tag,
                        OffsetDec = currentOffset,
                        OffsetHex = currentOffset.ToString("X") // or "X8" for leading zeros
                    };
                    tagEntries.Add(entry);
                }

                // Advance past the string + null terminator
                offset += tag.Length + 1;

                // Bounds check
                if (offset >= zoneBytes.Length)
                    break;
            }

            // If no tags were read, return null or an empty object
            if (tagEntries.Count == 0)
            {
                return null;
            }

            // Return them
            return new TagCollection
            {
                TagEntries = tagEntries
            };
        }
    }
}
