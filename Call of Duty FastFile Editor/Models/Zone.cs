using System.Net;
using System.Text;
using Call_of_Duty_FastFile_Editor.Services;
using Call_of_Duty_FastFile_Editor.ZoneParsers;

namespace Call_of_Duty_FastFile_Editor.Models
{
    public class Zone
    {
        public Zone(string zoneFilePath)
        {
            this.ZoneFilePath = zoneFilePath;
        }

        // The full path to the zone file.
        public string ZoneFilePath { get; private set; }

        /// <summary>
        /// Binary data of the zone file.
        /// </summary>
        public byte[] ZoneFileData { get; private set; }

        /// <summary>
        /// Modify the zone file and update the in-memory data.
        /// </summary>
        public void ModifyZoneFile(Action<FileStream> modification)
        {
            using (FileStream fs = new FileStream(ZoneFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                modification(fs);
            }
            RefreshZoneFileData();
        }

        /// <summary>
        /// Reads all bytes from the zone file and updates the in-memory ZoneFileData.
        /// </summary>
        public void RefreshZoneFileData()
        {
            ZoneFileData = File.ReadAllBytes(ZoneFilePath);
        }

        // Various zone header properties.
        public uint ZoneFileSize { get; set; }
        public uint Unknown1 { get; set; }
        public uint Unknown2 { get; set; }
        public uint Unknown3 { get; set; }
        public uint Unknown4 { get; set; }
        public uint Unknown5 { get; set; }
        public uint Unknown6 { get; set; }
        public uint Unknown7 { get; set; }
        public uint Unknown8 { get; set; }
        public uint TagCount { get; set; }
        public uint Unknown10 { get; set; }
        public uint AssetRecordCount { get; set; }

        // For display or debugging purposes.
        public Dictionary<string, uint>? DecimalValues { get; private set; }

        // The asset mapping container.
        public ZoneFileAssets ZoneFileAssets { get; set; } = new ZoneFileAssets();

        public int AssetPoolStartOffset { get; internal set; }
        public int AssetPoolEndOffset { get; internal set; }

        // Mapping of property names to their offsets (using your Constants).
        private readonly Dictionary<string, int> _zonePropertyOffsets = new Dictionary<string, int>
        {
            { "ZoneFileSize", Constants.ZoneFile.ZoneSizeOffset },
            { "Unknown1", Constants.ZoneFile.Unknown1Offset },
            { "Unknown2", Constants.ZoneFile.Unknown2Offset },
            { "Unknown3", Constants.ZoneFile.Unknown3Offset },
            { "Unknown4", Constants.ZoneFile.Unknown4Offset },
            { "Unknown5", Constants.ZoneFile.Unknown5Offset },
            { "Unknown6", Constants.ZoneFile.Unknown6Offset },
            { "Unknown7", Constants.ZoneFile.Unknown7Offset },
            { "Unknown8", Constants.ZoneFile.Unknown8Offset },
            { "TagCount", Constants.ZoneFile.TagCountOffset },
            { "Unknown10", Constants.ZoneFile.Unknown10Offset },
            { "AssetRecordCount", Constants.ZoneFile.AssetRecordCountOffset }
        };

        /// <summary>
        /// Reads all bytes from the zone file and stores them in ZoneFileData.
        /// </summary>
        public void GetSetZoneBytes()
        {
            this.ZoneFileData = File.ReadAllBytes(ZoneFilePath);
        }

        public void GetSetZoneAssetPool()
        {
            AssetPoolParser parser = new AssetPoolParser(this);
            parser.MapZoneAssetsPoolAndGetEndOffset();
        }

        /// <summary>
        /// Reads header values from the zone file using offsets from your Constants.
        /// </summary>
        public void SetZoneOffsets()
        {
            this.ZoneFileSize = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.ZoneSizeOffset, this);
            this.Unknown1 = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.Unknown1Offset, this);
            this.Unknown2 = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.Unknown2Offset, this);
            this.Unknown3 = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.Unknown3Offset, this);
            this.Unknown4 = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.Unknown4Offset, this);
            this.Unknown5 = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.Unknown5Offset, this);
            this.Unknown6 = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.Unknown6Offset, this);
            this.Unknown7 = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.Unknown7Offset, this);
            this.Unknown8 = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.Unknown8Offset, this);
            this.TagCount = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.TagCountOffset, this);
            this.Unknown10 = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.Unknown10Offset, this);
            this.AssetRecordCount = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.AssetRecordCountOffset, this);

            SetDecimalValues();
        }

        private void SetDecimalValues()
        {
            this.DecimalValues = new Dictionary<string, uint>()
            {
                { "ZoneFileSize", ZoneFileSize },
                { "Unknown1", Unknown1 },
                { "Unknown2", Unknown2 },
                { "Unknown3", Unknown3 },
                { "Unknown4", Unknown4 },
                { "Unknown5", Unknown5 },
                { "Unknown6", Unknown6 },
                { "Unknown7", Unknown7 },
                { "Unknown8", Unknown8 },
                { "TagCount", TagCount },
                { "Unknown10", Unknown10 },
                { "AssetRecordCount", AssetRecordCount }
            };
        }

        /// <summary>
        /// Retrieves the offset for a given zone property name in hexadecimal format.
        /// </summary>
        /// <param name="zoneName">The name of the zone property.</param>
        /// <returns>Hexadecimal string (e.g., "0x00") or "N/A" if not found.</returns>
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

        /// <summary>
        /// Reads the 4-byte zone file size from the header (big-endian) at the defined offset.
        /// </summary>
        public static uint ReadZoneFileSize(string zoneFilePath)
        {
            using (FileStream fs = new FileStream(zoneFilePath, FileMode.Open, FileAccess.Read))
            {
                fs.Seek(Constants.ZoneFile.ZoneSizeOffset, SeekOrigin.Begin);
                byte[] sizeBytes = new byte[4];
                fs.Read(sizeBytes, 0, sizeBytes.Length);
                // The size is stored in big-endian; reverse if necessary.
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(sizeBytes);
                return BitConverter.ToUInt32(sizeBytes, 0);
            }
        }

        /// <summary>
        /// Writes the updated zone file size (big-endian) to the header at the defined offset.
        /// </summary>
        public static void WriteZoneFileSize(string zoneFilePath, uint newZoneSize)
        {
            byte[] sizeBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)newZoneSize));
            using (FileStream fs = new FileStream(zoneFilePath, FileMode.Open, FileAccess.Write))
            {
                fs.Seek(Constants.ZoneFile.ZoneSizeOffset, SeekOrigin.Begin);
                fs.Write(sizeBytes, 0, sizeBytes.Length);
            }
        }
    }
}
