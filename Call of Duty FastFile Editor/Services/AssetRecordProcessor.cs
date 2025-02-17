using Call_of_Duty_FastFile_Editor.Models;
using Call_of_Duty_FastFile_Editor.ZoneParsers;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Call_of_Duty_FastFile_Editor.Services
{
    public static class AssetRecordProcessor
    {
        /// <summary>
        /// Processes the given zone asset records, extracting various asset types
        /// (like RawFileNode, StringTable, etc.) and returning them in an AssetProcessResult.
        /// </summary>
        public static AssetProcessResult ProcessAssetRecords(FastFile openedFastFile, List<ZoneAssetRecord> zoneAssetRecords)
        {
            // Create a result container to hold raw files, string tables, etc.
            AssetProcessResult result = new AssetProcessResult();

            bool previousRecordWasParsed = false;
            int indexOfLastAssetRecordParsed = 0;
            int previousRecordEndOffset = 0;
            int lastAssetRecordParsedEndOffset = 0;
            string assetRecordMethod;

            // Iterate over each asset record in the zone
            for (int i = 0; i < zoneAssetRecords.Count; i++)
            {
                assetRecordMethod = "";
                try
                {
                    if (i > 0)
                        previousRecordEndOffset = zoneAssetRecords[i - 1].AssetRecordEndOffset;

                    Debug.WriteLine("PreviousRecordEndOffset: " + previousRecordEndOffset + " Index: " + i);

                    // 1) Raw File
                    if (zoneAssetRecords[i].AssetType == ZoneFileAssetType.rawfile)
                    {
                        RawFileNode node = null;

                        // get rid of this eventually
                        if (i == 0)
                        {
                            Debug.WriteLine("Extracting raw file node from asset pool start offset.");
                            node = RawFileParser.ExtractSingleRawFileNodeNoPattern(
                                openedFastFile,
                                openedFastFile.OpenedFastFileZone.AssetPoolEndOffset
                            );

                            assetRecordMethod = "Updated in index 0 raw files using structure parsing, no pattern";
                        }
                        else if (previousRecordEndOffset > 0)
                        {
                            Debug.WriteLine($"Extracting raw file node from previous record's end offset: {previousRecordEndOffset}");
                            node = RawFileParser.ExtractSingleRawFileNodeNoPattern(openedFastFile, previousRecordEndOffset);

                            // this is a fallback to pattern matching when it has the FF FF FF FF,
                            // but the raw file's size is zero, hence its probably not a raw file following up right after
                            if (node == null)
                            {
                                node = RawFileParser.ExtractSingleRawFileNodeWithPattern(openedFastFile.ZoneFilePath, lastAssetRecordParsedEndOffset);
                                assetRecordMethod = "Fell back to pattern matching because raw file header not directly at the end.";
                            }
                            else
                            {
                                assetRecordMethod = "Updated using previous record's end point using no pattern";
                            }
                        }
                        else // fallback to pattern matching
                        {
                            lastAssetRecordParsedEndOffset = zoneAssetRecords[indexOfLastAssetRecordParsed].AssetRecordEndOffset;
                            node = RawFileParser.ExtractSingleRawFileNodeWithPattern(
                                openedFastFile.ZoneFilePath,
                                lastAssetRecordParsedEndOffset
                            );

                            assetRecordMethod = "Updated using pattern matching because previous record end was unknown";
                        }

                        if (node != null)
                        {
                            // Add to the result's RawFileNodes
                            result.RawFileNodes.Add(node);

                            UpdateAssetRecord(zoneAssetRecords, i, node, assetRecordMethod);
                            previousRecordWasParsed = true;
                            indexOfLastAssetRecordParsed = i;
                        }
                        else
                        {
                            previousRecordWasParsed = false;
                            assetRecordMethod = "Previous record was not parsed";
                        }
                    }
                    // 2) String Table
                    else if (zoneAssetRecords[i].AssetType == ZoneFileAssetType.stringtable)
                    {
                        StringTable stringTable = null;
                        if (previousRecordEndOffset > 0)
                        {
                            Debug.WriteLine($"Extracting string table from previous record's end offset: {previousRecordEndOffset}");
                            stringTable = StringTableParser.ParseStringTable(openedFastFile, previousRecordEndOffset);

                            assetRecordMethod = "Updated using previous record's end point using no pattern";
                        }
                        else
                        {
                            lastAssetRecordParsedEndOffset = zoneAssetRecords[indexOfLastAssetRecordParsed].AssetRecordEndOffset;
                            Debug.WriteLine($"Extracting string table from last asset record's end offset: {lastAssetRecordParsedEndOffset}");
                            stringTable = StringTable.FindSingleCsvStringTableWithPattern(
                                openedFastFile.OpenedFastFileZone,
                                lastAssetRecordParsedEndOffset
                            );

                            assetRecordMethod = "Updated using pattern matching because previous record end was unknown";
                        }

                        if (stringTable != null)
                        {
                            // Add to the result's StringTables
                            result.StringTables.Add(stringTable);

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

            // Store the updated zone asset records as well
            result.UpdatedRecords = zoneAssetRecords;

            return result;
        }

        /// <summary>
        /// Updates the zone asset record with the data from T (where T is an IAssetRecordUpdatable).
        /// Also sets an AdditionalData string for debugging.
        /// </summary>
        private static void UpdateAssetRecord<T>(List<ZoneAssetRecord> zoneAssetRecords,int index,T record,string assetRecordMethod) where T : IAssetRecordUpdatable
        {
            var assetRecord = zoneAssetRecords[index];
            record.UpdateAssetRecord(ref assetRecord);

            // Provide a debug string in the AdditionalData
            assetRecord.AdditionalData = assetRecordMethod;

            zoneAssetRecords[index] = assetRecord;

            Debug.WriteLine($"Updated asset record at index {index}: {assetRecord.Name}");
        }
    }
}
