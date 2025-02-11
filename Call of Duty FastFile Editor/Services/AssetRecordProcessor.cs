using Call_of_Duty_FastFile_Editor.Models;
using Call_of_Duty_FastFile_Editor.ZoneParsers;
using System;
using System.Diagnostics;

namespace Call_of_Duty_FastFile_Editor.Services
{
    public static class AssetRecordProcessor
    {
        public static List<RawFileNode> ProcessAssetRecords(FastFile openedFastFile, List<ZoneAssetRecord> zoneAssetRecords)
        {
            List<RawFileNode> rawFileNodes = new List<RawFileNode>();
            bool previousRecordWasParsed = false;
            int indexOfLastAssetRecordParsed = 0;
            int previousRecordEndOffset = 0;
            int lastAssetRecordParsedEndOffset = 0;
            string assetRecordMethod;

            for (int i = 0; i < zoneAssetRecords.Count; i++)
            {
                assetRecordMethod = "";
                try
                {
                    if (i > 0)
                        previousRecordEndOffset = zoneAssetRecords[i - 1].AssetDataEndOffset;

                    Debug.WriteLine("PreviousRecordEndOffset: " + previousRecordEndOffset + " Index: " + i);

                    if (zoneAssetRecords[i].AssetType == ZoneFileAssetType.rawfile)
                    {
                        RawFileNode node;

                        if (i == 0)
                        {
                            Debug.WriteLine("Extracting raw file node from asset pool start offset.");
                            node = RawFileParser.ExtractSingleRawFileNodeNoPattern(openedFastFile, openedFastFile.OpenedFastFileZone.AssetPoolEndOffset);

                            assetRecordMethod = "Updated in index 0 raw files using structure parsing, no pattern";
                        }
                        else if (previousRecordEndOffset > 0)
                        {
                            Debug.WriteLine($"Extracting raw file node from previous record's end offset: {previousRecordEndOffset}");
                            node = RawFileParser.ExtractSingleRawFileNodeNoPattern(openedFastFile, previousRecordEndOffset);

                            assetRecordMethod = "Updated using previous record's end point using no pattern";
                        }
                        else // temp fall back to parse with matching since we don't have previous record's end offset
                        {
                            lastAssetRecordParsedEndOffset = zoneAssetRecords[indexOfLastAssetRecordParsed].AssetDataEndOffset;
                            node = RawFileParser.ExtractSingleRawFileNodeWithPattern(openedFastFile.ZoneFilePath, lastAssetRecordParsedEndOffset);

                            assetRecordMethod = "Updated using pattern matching because previous record end was unknown";
                        }

                        if (node != null)
                        {
                            rawFileNodes.Add(node);
                            UpdateAssetRecord(zoneAssetRecords, i, node, assetRecordMethod);
                            previousRecordWasParsed = true;
                            indexOfLastAssetRecordParsed = i;
                        }
                        else
                        {
                            // this might not be needed. Maybe delete
                            previousRecordWasParsed = false;
                            assetRecordMethod = "Previous record was not parsed";
                        }
                    }
                    else if (zoneAssetRecords[i].AssetType == ZoneFileAssetType.stringtable)
                    {
                        StringTable stringTable;
                        if(previousRecordEndOffset > 0)
                        {
                            Debug.WriteLine($"Extracting string table from previous record's end offset: {previousRecordEndOffset}");
                            stringTable = StringTableParser.ParseStringTable(openedFastFile, previousRecordEndOffset);

                            assetRecordMethod = "Updated using previous record's end point using no pattern";
                        }
                        else // temp fall back to parse with matching since we don't have previous record's end offset
                        {
                            lastAssetRecordParsedEndOffset = zoneAssetRecords[indexOfLastAssetRecordParsed].AssetDataEndOffset;
                            Debug.WriteLine($"Extracting string table from last asset record's end offset: {lastAssetRecordParsedEndOffset}");
                            stringTable = StringTable.FindSingleCsvStringTableWithPattern(openedFastFile.OpenedFastFileZone, lastAssetRecordParsedEndOffset);

                            assetRecordMethod = "Updated using pattern matching because previous record end was unknown";
                        }

                        if (stringTable != null)
                        {
                            // Update the asset record for the string table.
                            UpdateAssetRecord(zoneAssetRecords, i, stringTable, assetRecordMethod);

                            indexOfLastAssetRecordParsed = i;
                        }
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

        private static void UpdateAssetRecord<T>(List<ZoneAssetRecord> zoneAssetRecords, int index, T record, string assetRecordMethod) where T : IAssetRecordUpdatable
        {
            var assetRecord = zoneAssetRecords[index];
            record.UpdateAssetRecord(ref assetRecord);

            // Set the AdditionalData field using the method string (for debugging)
            assetRecord.AdditionalData = assetRecordMethod;

            zoneAssetRecords[index] = assetRecord;

            Debug.WriteLine($"Updated asset record at index {index}: {assetRecord.Name}");
        }
    }
}