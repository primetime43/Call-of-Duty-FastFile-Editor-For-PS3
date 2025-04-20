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

            while (i <= fileLen - 8)
            {
                byte[] block = Utilities.GetBytesAtOffset(i, _zone, 8);

                if (foundAnyAsset && block.Take(4).All(b => b == 0xFF))
                {
                    Debug.WriteLine($"[AssetPoolRecordOffset {i}] Termination marker found.");
                    endOfPoolOffset = i;
                    break;
                }

                int assetTypeInt = (int)Utilities.ReadUInt32AtOffset(i, _zone, isBigEndian: true);
                if (!Enum.IsDefined(typeof(ZoneFileAssetType_COD5), assetTypeInt))
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
                    AssetType = (ZoneFileAssetType_COD5)assetTypeInt,
                    AssetPoolRecordOffset = i
                };

                _zone.ZoneFileAssets.ZoneAssetRecords.Add(record);
                foundAnyAsset = true;
                i += 8;
            }

            _zone.AssetPoolStartOffset = assetPoolStart;
            _zone.AssetPoolEndOffset = endOfPoolOffset;

            Debug.WriteLine($"[MapZoneAssetsPool] Found {_zone.ZoneFileAssets.ZoneAssetRecords.Count} records.");
            Debug.WriteLine($"[MapZoneAssetsPool] Start Offset: 0x{_zone.AssetPoolStartOffset:X}");
            Debug.WriteLine($"[MapZoneAssetsPool] End Offset: 0x{_zone.AssetPoolEndOffset:X}");
        }
    }
}
