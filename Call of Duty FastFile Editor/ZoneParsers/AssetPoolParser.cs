using System.Diagnostics;
using Call_of_Duty_FastFile_Editor.Models;
using Call_of_Duty_FastFile_Editor.Services;

namespace Call_of_Duty_FastFile_Editor.ZoneParsers
{
    public class AssetPoolParser
    {
        private ZoneFile _zone;

        public AssetPoolParser(ZoneFile zone)
        {
            _zone = zone;
        }

        public void MapZoneAssetsPoolAndGetEndOffset()
        {
            if (_zone.ZoneFileAssets.ZoneAssetRecords == null)
                _zone.ZoneFileAssets.ZoneAssetRecords = new List<ZoneAssetRecord>();
            _zone.ZoneFileAssets.ZoneAssetRecords.Clear();

            byte[] data = _zone.Data;
            int fileLen = data.Length;
            int i = 0;
            bool foundAnyAsset = false;
            int assetPoolStart = -1;
            int endOfPoolOffset = -1;

            // Detect game
            bool isCod4 = _zone.ParentFastFile?.IsCod4File ?? false;
            bool isCod5 = _zone.ParentFastFile?.IsCod5File ?? false;

            // Use AssetRecordCount from header (for both games!)
            int expectedEntries = (int)_zone.AssetRecordCount;

            Debug.WriteLine("Number of assets expected: " + expectedEntries);

            int assetCount = 0;
            while (i <= fileLen - 8)
            {
                // For CoD5: if you want to keep the all-FFs end marker, you can, but it's not needed if you trust AssetRecordCount.
                // You may still want to check for all-FFs as a sanity check, up to you.
                if (assetCount >= expectedEntries)
                {
                    endOfPoolOffset = i;
                    break;
                }

                byte[] block = Utilities.GetBytesAtOffset(i, _zone, 8);

                int assetTypeInt = (int)Utilities.ReadUInt32AtOffset(i, _zone, isBigEndian: true);

                // Use correct enum depending on game
                bool isDefined = false;
                if (isCod4)
                    isDefined = Enum.IsDefined(typeof(ZoneFileAssetType_COD4), assetTypeInt);
                else if (isCod5)
                    isDefined = Enum.IsDefined(typeof(ZoneFileAssetType_COD5), assetTypeInt);

                if (!isDefined)
                {
                    i++;
                    continue;
                }

                byte[] paddingBytes = Utilities.GetBytesAtOffset(i + 4, _zone, 4);
                if (!paddingBytes.All(b => b == 0xFF))
                {
                    i++;
                    continue;
                }

                if (!foundAnyAsset)
                    assetPoolStart = i;

                var record = new ZoneAssetRecord
                {
                    AssetPoolRecordOffset = i
                };

                if (isCod4)
                    record.AssetType_COD4 = (ZoneFileAssetType_COD4)assetTypeInt;
                else if (isCod5)
                    record.AssetType_COD5 = (ZoneFileAssetType_COD5)assetTypeInt;

                _zone.ZoneFileAssets.ZoneAssetRecords.Add(record);
                foundAnyAsset = true;
                i += 8;
                assetCount++;
            }

            _zone.AssetPoolStartOffset = assetPoolStart;
            _zone.AssetPoolEndOffset = endOfPoolOffset;

            Debug.WriteLine($"[MapZoneAssetsPool] Found {_zone.ZoneFileAssets.ZoneAssetRecords.Count} records.");
            Debug.WriteLine($"[MapZoneAssetsPool] Start Offset: 0x{_zone.AssetPoolStartOffset:X}");
            Debug.WriteLine($"[MapZoneAssetsPool] End Offset: 0x{_zone.AssetPoolEndOffset:X}");
        }
    }
}
