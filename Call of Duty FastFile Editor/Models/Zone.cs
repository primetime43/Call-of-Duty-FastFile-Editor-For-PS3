using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using Call_of_Duty_FastFile_Editor.Services;

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

        // Various zone header properties.
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

        /// <summary>
        /// A convenience method to load zone data and then map the assets.
        /// </summary>
        public void LoadZoneAssetsPool()
        {
            SetZoneData();
            MapZoneAssetsPool();
        }

        // For display or debugging purposes.
        public Dictionary<string, uint>? DecimalValues { get; private set; }

        // The asset mapping container.
        public ZoneFileAssets ZoneFileAssets { get; set; } = new ZoneFileAssets();

        // Mapping of property names to their offsets (using your Constants).
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
        /// Reads all bytes from the zone file and stores them in ZoneFileData.
        /// </summary>
        public void SetZoneData()
        {
            this.ZoneFileData = File.ReadAllBytes(ZoneFilePath);
        }

        /// <summary>
        /// Reads header values from the zone file using offsets from your Constants.
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
            this.TagCount = Utilities.ReadUInt32AtOffset(Constants.ZoneFile.TagCountOffset, this);
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
        /// Maps asset records from the zone file data.
        /// The asset pool is stored at the start of the zone file. The RecordCount property (set from the header)
        /// tells you how many 8‑byte records to read. Each record consists of 4 bytes for the asset type and 4 bytes for additional data.
        /// </summary>
        private void MapZoneAssetsPool()
        {
            if (ZoneFileAssets.ZoneAssetsPool == null)
                ZoneFileAssets.ZoneAssetsPool = new List<ZoneAssetRecord>();

            // Clear old if needed
            ZoneFileAssets.ZoneAssetsPool.Clear();

            byte[] data = ZoneFileData;
            int fileLen = data.Length;

            // We'll walk 'i' manually so we can skip different amounts
            int i = 0;

            // Flag to know if we've found any valid asset record yet
            bool foundAnyAsset = false;

            // We only need to scan while there's enough room for at least 8 bytes
            while (i <= fileLen - 8)
            {
                // If we've already found at least 1 record, check if the next 8 are all FF
                if (foundAnyAsset)
                {
                    byte[] next8 = Utilities.GetBytesAtOffset(i, this, 8);
                    bool allFF = true;
                    for (int j = 0; j < 8; j++)
                    {
                        if (next8[j] != 0xFF)
                        {
                            allFF = false;
                            break;
                        }
                    }

                    // If the next 8 bytes are all 0xFF, end the scan
                    if (allFF)
                    {
                        Debug.WriteLine("Encountered 8 consecutive FF bytes. Ending asset scan here.");
                        break;
                    }
                }

                // Read the first 4 bytes as big-endian
                uint rawValue = Utilities.ReadUInt32AtOffset(i, this, isBigEndian: true);
                int assetTypeInt = (int)rawValue;

                // Check if it matches a valid enum entry
                if (!Enum.IsDefined(typeof(ZoneFileAssetType), assetTypeInt))
                {
                    // Not a valid asset; skip forward by 1 byte and keep looking
                    i++;
                    continue;
                }

                // If it's a valid asset type, check the next 4 bytes for FF FF FF FF
                byte[] paddingBytes = Utilities.GetBytesAtOffset(i + 4, this, 4);
                bool hasPadding =
                    paddingBytes[0] == 0xFF &&
                    paddingBytes[1] == 0xFF &&
                    paddingBytes[2] == 0xFF &&
                    paddingBytes[3] == 0xFF;

                if (hasPadding)
                {
                    // We found a valid "AssetType + FF FF FF FF" record
                    var record = new ZoneAssetRecord
                    {
                        AssetType = (ZoneFileAssetType)assetTypeInt,
                        AdditionalData = 0,
                        Offset = i
                    };
                    ZoneFileAssets.ZoneAssetsPool.Add(record);

                    foundAnyAsset = true;

                    // Jump past these 8 bytes
                    i += 8;
                }
                else
                {
                    // The first 4 bytes matched an asset type, but the next 4
                    // weren't FF FF FF FF => not a complete record
                    i++;
                }
            }

            // Group by type for per-type counts
            var groupByType = ZoneFileAssets.ZoneAssetsPool
                .GroupBy(r => r.AssetType)
                .Select(g => new { AssetType = g.Key, Count = g.Count() });

            foreach (var group in groupByType)
            {
                Debug.WriteLine($"[MapZoneAssetsPool] AssetType {group.AssetType} => {group.Count} record(s).");
            }

            Debug.WriteLine($"[MapZoneAssetsPool] Found {ZoneFileAssets.ZoneAssetsPool.Count} asset record(s) total.");
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

        /// <summary>
        /// Returns a formatted string containing the zone header information.
        /// </summary>
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
    }
}
