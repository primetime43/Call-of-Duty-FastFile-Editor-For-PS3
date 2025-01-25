using Call_of_Duty_FastFile_Editor.Services;
using System.Text;


namespace Call_of_Duty_FastFile_Editor.Models
{    
    public class Zone
    {
        /// <summary>
        /// Binary data of the zone file
        /// </summary>
        public byte[] FileData { get; set; }
        public uint ZoneFileSize { get; set; }
        public uint Unknown1 { get; set; }
        public uint RecordCount { get; set; }
        public uint Unknown3 { get; set; }
        public uint Unknown4 { get; set; }
        public uint Unknown5 { get; set; }
        public uint Unknown6 { get; set; }
        public uint Unknown7 { get; set; }
        public uint Unknown8 { get; set; }
        public uint TagCount { get; set; }
        public uint Unknown10 { get; set; }
        public uint Unknown11 { get; set; }
        public List<uint> TagPtrs { get; set; } = new List<uint>();

        public Dictionary<string, uint>? DecimalValues { get; private set; }

        public ZoneFileAssets ZoneFileAssets { get; set; } = new ZoneFileAssets();

        // Mapping of property names to their respective offsets
        private readonly Dictionary<string, int> _zonePropertyOffsets = new Dictionary<string, int>
        {
            { "ZoneFileSize", Constants.ZoneFile.ZoneSizeOffset },
            { "Unknown1", Constants.ZoneFile.Unknown1Offset },
            { "RecordCount", Constants.ZoneFile.RecordCountOffset },
            { "Unknown3", Constants.ZoneFile.Unknown3Offset },
            { "Unknown4", Constants.ZoneFile.Unknown4Offset },
            { "Unknown5", Constants.ZoneFile.Unknown5Offset },
            { "Unknown6", Constants.ZoneFile.Unknown6Offset },
            { "Unknown7", Constants.ZoneFile.Unknown7Offset },
            { "Unknown8", Constants.ZoneFile.Unknown8Offset },
            { "TagCount", Constants.ZoneFile.TagCountOffset },
            { "Unknown10", Constants.ZoneFile.Unknown10Offset },
            { "Unknown11", Constants.ZoneFile.Unknown11Offset }
        };

        /// <summary>
        /// Sets the values of locations in the zone based off the offsets from Constants
        /// Rename maybe?
        /// </summary>
        public void SetZoneOffsets()
        {
            this.ZoneFileSize = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.ZoneSizeOffset, this);
            this.Unknown1 = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.Unknown1Offset, this);
            this.RecordCount = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.RecordCountOffset, this);
            this.Unknown3 = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.Unknown3Offset, this);
            this.Unknown4 = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.Unknown4Offset, this);
            this.Unknown5 = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.Unknown5Offset, this);
            this.Unknown6 = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.Unknown6Offset, this);
            this.Unknown7 = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.Unknown7Offset, this);
            this.Unknown8 = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.Unknown8Offset, this);
            //this.TagCount = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.TagCountOffset, this);
            this.TagCount = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.TagCountOffset, this);
            //this.RecordCount = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.RecordCountOffset, this);
            this.Unknown10 = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.Unknown10Offset, this);
            this.Unknown11 = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.Unknown11Offset, this);

            SetDecimalValues();
        }

        private void SetDecimalValues()
        {
            this.DecimalValues = new Dictionary<string, uint>()
            {
                { "ZoneFileSize", ZoneFileSize },
                { "Unknown1", Unknown1 },
                { "RecordCount", RecordCount },
                { "Unknown3", Unknown3 },
                { "Unknown4", Unknown4 },
                { "Unknown5", Unknown5 },
                { "Unknown6", Unknown6 },
                { "Unknown7", Unknown7 },
                { "Unknown8", Unknown8 },
                { "TagCount", TagCount },
                { "Unknown10", Unknown10 },
                { "Unknown11", Unknown11 }
            };
        }

        /// <summary>
        /// Provides a formatted string of all relevant properties with their decimal values
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"ZoneFileSize: {ZoneFileSize}");
            sb.AppendLine($"Unknown1: {Unknown1}");
            sb.AppendLine($"RecordCount: {RecordCount}");
            sb.AppendLine($"Unknown3: {Unknown3}");
            sb.AppendLine($"Unknown4: {Unknown4}");
            sb.AppendLine($"Unknown5: {Unknown5}");
            sb.AppendLine($"Unknown6: {Unknown6}");
            sb.AppendLine($"Unknown7: {Unknown7}");
            sb.AppendLine($"Unknown8: {Unknown8}");
            sb.AppendLine($"TagCount: {TagCount}");
            sb.AppendLine($"Unknown10: {Unknown10}");
            sb.AppendLine($"Unknown11: {Unknown11}");

            return sb.ToString();
        }

        /// <summary>
        /// Retrieves the offset for a given zone property name.
        /// </summary>
        /// <param name="zoneName">The name of the zone property.</param>
        /// <returns>A string representing the offset in hexadecimal format (e.g., "0x00").</returns>
        public string GetZoneOffset(string zoneName)
        {
            if (_zonePropertyOffsets.TryGetValue(zoneName, out int offset))
            {
                return $"0x{offset:X2}";
            }
            else
            {
                return "N/A";
            }
        }
    }
}
