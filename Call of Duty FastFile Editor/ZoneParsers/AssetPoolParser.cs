using System.Diagnostics;
using Call_of_Duty_FastFile_Editor.Models;
using Call_of_Duty_FastFile_Editor.Services;

namespace Call_of_Duty_FastFile_Editor.ZoneParsers
{
    public class AssetPoolParser
    {
        private readonly ZoneFile _zone;

        public AssetPoolParser(ZoneFile zone)
        {
            _zone = zone;
        }

        public bool MapZoneAssetsPoolAndGetEndOffset()
        {
            var records = _zone.ZoneFileAssets.ZoneAssetRecords ?? new List<ZoneAssetRecord>();
            records.Clear();
            _zone.ZoneFileAssets.ZoneAssetRecords = records;

            byte[] data = _zone.Data;
            int fileLen = data.Length;
            int i;

            // Detect game
            bool isCod4 = _zone.ParentFastFile?.IsCod4File ?? false;
            bool isCod5 = _zone.ParentFastFile?.IsCod5File ?? false;

            // Start after tags, or at file start
            var tags = TagOperations.FindTags(_zone);
            i = (tags != null && tags.TagSectionEndOffset > 0) ? tags.TagSectionEndOffset : 0;

            bool foundAnyAsset = false;
            int assetPoolStart = -1;
            int endOfPoolOffset = -1;

            while (i <= fileLen - 8)
            {
                // End marker: 0xFF 0xFF 0xFF 0xFF
                if (data[i] == 0xFF && data[i + 1] == 0xFF && data[i + 2] == 0xFF && data[i + 3] == 0xFF)
                {
                    Debug.WriteLine($"[DEBUG] END MARKER FF FF FF FF at 0x{i:X}, breaking.");
                    endOfPoolOffset = i;
                    break;
                }

                Debug.WriteLine($"[DEBUG] i=0x{i:X} | {data[i]:X2} {data[i + 1]:X2} {data[i + 2]:X2} {data[i + 3]:X2} {data[i + 4]:X2} {data[i + 5]:X2} {data[i + 6]:X2} {data[i + 7]:X2}");

                // Asset record: 00 00 00 XX FF FF FF FF
                if (data[i] == 0x00 && data[i + 1] == 0x00 && data[i + 2] == 0x00
                    && data[i + 4] == 0xFF && data[i + 5] == 0xFF && data[i + 6] == 0xFF && data[i + 7] == 0xFF)
                {
                    int assetTypeInt = (int)Utilities.ReadUInt32AtOffset(i, _zone, isBigEndian: true);
                    bool isDefined =
                        (isCod4 && Enum.IsDefined(typeof(ZoneFileAssetType_COD4), assetTypeInt)) ||
                        (isCod5 && Enum.IsDefined(typeof(ZoneFileAssetType_COD5), assetTypeInt));

                    Debug.WriteLine($"[DEBUG] Found potential asset at 0x{i:X}: assetType=0x{assetTypeInt:X} defined={isDefined}");

                    if (isDefined)
                    {
                        if (!foundAnyAsset) assetPoolStart = i;

                        var record = new ZoneAssetRecord { AssetPoolRecordOffset = i };
                        if (isCod4) record.AssetType_COD4 = (ZoneFileAssetType_COD4)assetTypeInt;
                        else if (isCod5) record.AssetType_COD5 = (ZoneFileAssetType_COD5)assetTypeInt;
                        records.Add(record);

                        foundAnyAsset = true;
                        i += 8; // skip to next possible record
                        continue;
                    }
                }
                i++;
            }

            if (endOfPoolOffset == -1)
                endOfPoolOffset = i;

            _zone.AssetPoolStartOffset = assetPoolStart;
            _zone.AssetPoolEndOffset = endOfPoolOffset;

            Debug.WriteLine($"[MapZoneAssetsPool] Found {records.Count} records.");
            Debug.WriteLine($"[MapZoneAssetsPool] Start Offset: 0x{_zone.AssetPoolStartOffset:X}");
            Debug.WriteLine($"[MapZoneAssetsPool] End Offset: 0x{_zone.AssetPoolEndOffset:X}");

            return foundAnyAsset;
        }
    }
}
