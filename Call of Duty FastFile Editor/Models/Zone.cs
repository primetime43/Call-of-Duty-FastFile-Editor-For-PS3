using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Policy;
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

        // For display or debugging purposes.
        public Dictionary<string, uint>? DecimalValues { get; private set; }

        // The asset mapping container.
        public ZoneFileAssets ZoneFileAssets { get; set; } = new ZoneFileAssets();

        public int AssetPoolStartOffset { get; private set; }
        public int AssetPoolEndOffset { get; private set; }

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
        /// Finds the start and end of the asset pool in the zone file.
        /// Catalogs the asset records and their order.
        /// </summary>
        public void MapZoneAssetsPoolAndGetEndOffset()
        {
            if (ZoneFileAssets.ZoneAssetsPool == null)
                ZoneFileAssets.ZoneAssetsPool = new List<ZoneAssetRecord>();
            ZoneFileAssets.ZoneAssetsPool.Clear();

            byte[] data = ZoneFileData;
            int fileLen = data.Length;
            int i = 0;
            bool foundAnyAsset = false;
            int assetPoolStart = -1;  // We'll capture the first valid asset record offset here.
            int endOfPoolOffset = -1; // Termination marker offset.

            while (i <= fileLen - 8)
            {
                // Read the next 8-byte block.
                byte[] block = Utilities.GetBytesAtOffset(i, this, 8);

                // Only check for termination marker if we've found at least one asset.
                if (foundAnyAsset)
                {
                    if (block[0] == 0xFF && block[1] == 0xFF && block[2] == 0xFF && block[3] == 0xFF)
                    {
                        Debug.WriteLine($"[AssetPoolRecordOffset {i}] Termination marker found: first 4 bytes are all FF. Ending asset pool.");
                        endOfPoolOffset = i;
                        break;
                    }
                }

                // Try reading the asset type from the first 4 bytes.
                int assetTypeInt = (int)Utilities.ReadUInt32AtOffset(i, this, isBigEndian: true);

                // If the asset type isn’t defined in our enum, advance one byte.
                if (!Enum.IsDefined(typeof(ZoneFileAssetType), assetTypeInt))
                {
                    i++;
                    continue;
                }

                // Next 4 bytes should be padding (FF FF FF FF).
                byte[] paddingBytes = Utilities.GetBytesAtOffset(i + 4, this, 4);
                bool paddingValid =
                    paddingBytes[0] == 0xFF &&
                    paddingBytes[1] == 0xFF &&
                    paddingBytes[2] == 0xFF &&
                    paddingBytes[3] == 0xFF;
                if (!paddingValid)
                {
                    Debug.WriteLine($"[AssetPoolRecordOffset {i}] Padding bytes are not all FF. Advancing one byte.");
                    i++;
                    continue;
                }

                // If this is the first valid asset record, record its offset.
                if (!foundAnyAsset)
                {
                    assetPoolStart = i;
                }

                Debug.WriteLine($"[AssetPoolRecordOffset {i}] Found valid asset record: AssetType = {assetTypeInt}.");
                var record = new ZoneAssetRecord
                {
                    AssetType = (ZoneFileAssetType)assetTypeInt,
                    AdditionalData = 0,
                    AssetPoolRecordOffset = i
                };
                ZoneFileAssets.ZoneAssetsPool.Add(record);
                foundAnyAsset = true;
                i += 8; // Skip the 8-byte block for this record.
            }

            // Set the Zone's asset pool offsets.
            this.AssetPoolStartOffset = assetPoolStart;
            this.AssetPoolEndOffset = endOfPoolOffset;

            // Print debugging info.
            var groupByType = ZoneFileAssets.ZoneAssetsPool
                .GroupBy(r => r.AssetType)
                .Select(g => new { AssetType = g.Key, Count = g.Count() });
            foreach (var group in groupByType)
            {
                Debug.WriteLine($"[MapZoneAssetsPool] AssetType {group.AssetType} => {group.Count} record(s).");
            }
            Debug.WriteLine($"[MapZoneAssetsPool] Found {ZoneFileAssets.ZoneAssetsPool.Count} asset record(s) total.");
            Debug.WriteLine($"[MapZoneAssetsPool] Asset pool start offset: 0x{AssetPoolStartOffset:X}");
            Debug.WriteLine($"[MapZoneAssetsPool] Asset pool end offset: 0x{AssetPoolEndOffset:X}");
        }

        public void ScanForAssetData(int assetPoolEndOffset)
        {
            if (assetPoolEndOffset < 0)
            {
                Debug.WriteLine("[ScanForAssetData] No 8xFF marker found to end the pool. Nothing to scan.");
                return;
            }

            // If we have N records, we only want to parse N data blocks
            int totalRecords = ZoneFileAssets.ZoneAssetsPool?.Count ?? 0;
            if (totalRecords == 0)
            {
                Debug.WriteLine("[ScanForAssetData] No asset records found, skipping data scan.");
                return;
            }

            // The data blocks come after 'assetPoolEndOffset + 8'
            int contentStart = assetPoolEndOffset + 8;

            // We'll parse exactly 'totalRecords' blocks
            var blocks = ExtractDataBlocksAfterPool(contentStart, totalRecords);

            // Now line up each data block with each record
            for (int i = 0; i < blocks.Count; i++)
            {
                // If more blocks than records, just break
                if (i >= ZoneFileAssets.ZoneAssetsPool.Count)
                    break;

                var record = ZoneFileAssets.ZoneAssetsPool[i];
                var block = blocks[i];

                // Store as raw bytes in the record
                record.RawDataBytes = block.Content;
                record.Size = block.Content.Length;

                // Convert raw bytes to string if localize
                if(ZoneFileAssets.ZoneAssetsPool[i].AssetType == ZoneFileAssetType.localize)
                    record.Content = Encoding.UTF8.GetString(block.Content);

                record.AssetDataStartPosition = block.StartOffset;
                record.AssetDataEndOffset = block.EndOffset;

                // Keep the original asset type, do NOT overwrite record.AssetType
                record.AssetType = ZoneFileAssets.ZoneAssetsPool[i].AssetType; // Remove or comment out this line

                ZoneFileAssets.ZoneAssetsPool[i] = record;

                Debug.WriteLine($"[ScanForAssetData] Asset[{i}] {record.AssetType}, dataLen={record.Size}, offset=0x{record.AssetDataStartPosition:X}-0x{record.AssetDataEndOffset:X}");
            }

            BuildAssetsByTypeMap();

            Debug.WriteLine("[ScanForAssetData] Finished scanning asset data.");
        }

        /// <summary>
        /// After we know where the asset pool ended, we can parse
        /// everything *after* that offset in separate data blocks
        /// that each end at 8 consecutive FF.
        /// </summary>
        public List<ZoneDataBlock> ExtractDataBlocksAfterPool(int startOffset, int maxBlocksToRead)
        {
            List<ZoneDataBlock> blocks = new List<ZoneDataBlock>();

            byte[] data = ZoneFileData;
            int fileLen = data.Length;
            int currentPos = startOffset;

            // We'll only read up to 'maxBlocksToRead' blocks
            int blocksFound = 0;

            while (blocksFound < maxBlocksToRead && currentPos < fileLen)
            {
                // If not enough space left for 8 bytes, treat the remaining as one final block
                if (currentPos > fileLen - 8)
                {
                    int remainingLen = fileLen - currentPos;
                    var finalBlock = new ZoneDataBlock
                    {
                        StartOffset = currentPos,
                        EndOffset = fileLen - 1,
                        Content = Utilities.GetBytesAtOffset(currentPos, this, remainingLen)
                    };
                    blocks.Add(finalBlock);
                    break;
                }

                // Check next 8 bytes for FF FF FF FF FF FF FF FF
                byte[] next8 = Utilities.GetBytesAtOffset(currentPos, this, 8);
                bool allFF = true;
                for (int i = 0; i < 8; i++)
                {
                    if (next8[i] != 0xFF)
                    {
                        allFF = false;
                        break;
                    }
                }

                if (allFF)
                {
                    // We found 8xFF with no preceding data => skip it
                    currentPos += 8;
                    continue;
                }

                // This is the start of a new data block
                int blockStart = currentPos;
                int blockEnd = -1;

                // Move forward until we see 8xFF or EOF
                while (currentPos <= fileLen - 8)
                {
                    byte[] check8 = Utilities.GetBytesAtOffset(currentPos, this, 8);
                    bool foundTerminator = true;
                    for (int j = 0; j < 8; j++)
                    {
                        if (check8[j] != 0xFF)
                        {
                            foundTerminator = false;
                            break;
                        }
                    }
                    if (foundTerminator)
                    {
                        // block ends just before these 8 FF
                        blockEnd = currentPos - 1;
                        break;
                    }
                    currentPos++;
                }

                if (blockEnd == -1)
                {
                    // never found 8xFF => go to end of file
                    blockEnd = fileLen - 1;
                }

                int length = (blockEnd - blockStart + 1);
                byte[] blockData = Utilities.GetBytesAtOffset(blockStart, this, length);

                var block = new ZoneDataBlock
                {
                    StartOffset = blockStart,
                    EndOffset = blockEnd,
                    Content = blockData
                };
                blocks.Add(block);

                blocksFound++;

                // Move currentPos just past the 8xFF, if it exists
                if (blockEnd < fileLen - 8)
                    currentPos = blockEnd + 1 + 8;
                else
                    currentPos = fileLen;
            }

            return blocks;
        }

        private void BuildAssetsByTypeMap()
        {
            // Clear the dictionary so we don’t duplicate
            ZoneFileAssets.AssetsByType.Clear();

            // If there’s nothing in ZoneAssetsPool, we’re done
            if (ZoneFileAssets.ZoneAssetsPool == null ||
                ZoneFileAssets.ZoneAssetsPool.Count == 0)
            {
                return;
            }

            // For each record, add it to the dictionary’s list
            foreach (var record in ZoneFileAssets.ZoneAssetsPool)
            {
                // if the dictionary does NOT yet have an entry for this assetType,
                // create a new list and store it
                if (!ZoneFileAssets.AssetsByType.TryGetValue(record.AssetType, out var list))
                {
                    list = new List<ZoneAssetRecord>();
                    ZoneFileAssets.AssetsByType[record.AssetType] = list;
                }

                list.Add(record);
            }
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

    /// <summary>
    /// Represents a chunk of data that lies between two 8xFF markers.
    /// </summary>
    public class ZoneDataBlock
    {
        public int StartOffset { get; set; }
        public int EndOffset { get; set; }
        public byte[] Content { get; set; }
    }

}
