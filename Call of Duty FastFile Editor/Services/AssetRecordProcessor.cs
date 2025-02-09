using Call_of_Duty_FastFile_Editor.Models;
using Call_of_Duty_FastFile_Editor.ZoneParsers;
using System.Diagnostics;

namespace Call_of_Duty_FastFile_Editor.Services
{
    public static class AssetRecordProcessor
    {
        public static List<RawFileNode> ProcessAssetRecords(FastFile openedFastFile, List<ZoneAssetRecord> zoneAssetRecords)
        {
            List<RawFileNode> rawFileNodes = new List<RawFileNode>();
            bool previousRecordWasParsed = false;
            int indexOfLastRawFileParsed = 0;

            for (int i = 0; i < zoneAssetRecords.Count; i++)
            {
                try
                {
                    if (zoneAssetRecords[i].AssetType == ZoneFileAssetType.rawfile)
                    {
                        RawFileNode node;

                        if (i == 0)
                        {
                            node = RawFileParser.ExtractSingleRawFileNodeNoPattern(openedFastFile, openedFastFile.OpenedFastFileZone.AssetPoolEndOffset);
                        }
                        else if (zoneAssetRecords[i - 1].AssetType == ZoneFileAssetType.rawfile)
                        {
                            int previousRecordEndOffset = zoneAssetRecords[i - 1].AssetDataEndOffset;
                            node = RawFileParser.ExtractSingleRawFileNodeNoPattern(openedFastFile, previousRecordEndOffset);
                        }
                        else
                        {
                            int lastRawFileRecordEndOffset = zoneAssetRecords[indexOfLastRawFileParsed].AssetDataEndOffset;
                            node = RawFileParser.ExtractSingleRawFileNodeWithPattern(openedFastFile.ZoneFilePath, lastRawFileRecordEndOffset);
                        }

                        if (node != null)
                        {
                            rawFileNodes.Add(node);
                            UpdateAssetRecord(zoneAssetRecords, i, node);
                            previousRecordWasParsed = true;
                            indexOfLastRawFileParsed = i;
                        }
                        else
                        {
                            previousRecordWasParsed = false;
                        }
                    }
                    else if (zoneAssetRecords[i].AssetType == ZoneFileAssetType.stringtable && zoneAssetRecords[i - 1].AssetDataEndOffset != 0)
                    {
                        int previousRecordEndOffset = zoneAssetRecords[i - 1].AssetDataEndOffset;
                        StringTable stringTable = StringTableParser.ParseStringTable(openedFastFile, previousRecordEndOffset);

                        // Update the asset record for the string table.
                        UpdateAssetRecord(zoneAssetRecords, i, stringTable);
                    }
                    else
                    {
                        previousRecordWasParsed = false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to process asset record at index {i}: {ex.Message}");
                }
            }

            return rawFileNodes;
        }

        private static void UpdateAssetRecord<T>(List<ZoneAssetRecord> zoneAssetRecords, int index, T record) where T : IAssetRecordUpdatable
        {
            var assetRecord = zoneAssetRecords[index];
            record.UpdateAssetRecord(ref assetRecord);
            zoneAssetRecords[index] = assetRecord;

            Debug.WriteLine($"Updated asset record at index {index}: {assetRecord.Name}");
        }
    }
}