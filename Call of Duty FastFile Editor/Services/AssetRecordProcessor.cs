using Call_of_Duty_FastFile_Editor.Models;
using Call_of_Duty_FastFile_Editor.ZoneParsers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace Call_of_Duty_FastFile_Editor.Services
{
    public static class AssetRecordProcessor
    {
        /// <summary>
        /// Processes the given zone asset records, extracting various asset types
        /// (like RawFileNode, StringTable, and Localize) and returning them in a ZoneAssetRecords.
        /// </summary>
        public static ZoneAssetRecords ProcessAssetRecords(FastFile openedFastFile, List<ZoneAssetRecord> zoneAssetRecords)
        {
            // Create a result container.
            ZoneAssetRecords result = new ZoneAssetRecords();

            int indexOfLastAssetRecordParsed = 0;
            int previousRecordEndOffset = 0;
            string assetRecordMethod = string.Empty;

            // Iterate over each asset record.
            for (int i = 0; i < zoneAssetRecords.Count; i++)
            {
                try
                {
                    if (i > 0)
                        previousRecordEndOffset = zoneAssetRecords[i - 1].AssetRecordEndOffset;

                    Debug.WriteLine($"Processing record index {i}, previousRecordEndOffset: {previousRecordEndOffset}");

                    // Uniformly determine the starting offset.
                    int startingOffset = (i == 0)
                        ? openedFastFile.OpenedFastFileZone.AssetPoolEndOffset
                        : (previousRecordEndOffset > 0
                            ? previousRecordEndOffset
                            : zoneAssetRecords[indexOfLastAssetRecordParsed].AssetRecordEndOffset);

                    switch (zoneAssetRecords[i].AssetType)
                    {
                        case ZoneFileAssetType.rawfile:
                            {
                                // Try no-pattern method first, fall back to pattern.
                                RawFileNode node = RawFileParser.ExtractSingleRawFileNodeNoPattern(openedFastFile, startingOffset)
                                    ?? RawFileParser.ExtractSingleRawFileNodeWithPattern(openedFastFile.ZoneFilePath, startingOffset);

                                if (node != null)
                                {
                                    assetRecordMethod = (previousRecordEndOffset > 0)
                                        ? "Raw file parsed using previous record's offset."
                                        : "Raw file parsed pattern matching because previous record end was unknown.";
                                    result.RawFileNodes.Add(node);
                                    UpdateAssetRecord(zoneAssetRecords, i, node, assetRecordMethod);
                                    indexOfLastAssetRecordParsed = i;
                                }
                                break;
                            }
                        case ZoneFileAssetType.stringtable:
                            {
                                StringTable table = (previousRecordEndOffset > 0)
                                    ? StringTableParser.ParseStringTable(openedFastFile, startingOffset)
                                    : StringTable.FindSingleCsvStringTableWithPattern(openedFastFile.OpenedFastFileZone, startingOffset);

                                if (table != null)
                                {
                                    assetRecordMethod = (previousRecordEndOffset > 0)
                                        ? "String table parsed using previous record's offset."
                                        : "String table parsed using pattern matching because previous record end was unknown.";
                                    result.StringTables.Add(table);
                                    UpdateAssetRecord(zoneAssetRecords, i, table, assetRecordMethod);
                                    indexOfLastAssetRecordParsed = i;
                                }
                                break;
                            }
                        case ZoneFileAssetType.localize:
                            {
                                // Use a ternary operator to select the appropriate parsing method.
                                var tuple = (previousRecordEndOffset > 0)
                                    ? LocalizeAssetParser.ParseSingleLocalizeAssetNoPattern(openedFastFile, startingOffset)
                                    : LocalizeAssetParser.ParseSingleLocalizeAssetWithPattern(openedFastFile, startingOffset);

                                LocalizedEntry localizedEntry = tuple.entry;
                                assetRecordMethod = (previousRecordEndOffset > 0)
                                    ? "Localized asset parsed using previous record's offset."
                                    : "Localized asset parsed using pattern matching because previous record end was unknown.";

                                if (localizedEntry != null)
                                {
                                    result.LocalizedEntries.Add(localizedEntry);
                                    UpdateAssetRecord(zoneAssetRecords, i, localizedEntry, assetRecordMethod);
                                    indexOfLastAssetRecordParsed = i;
                                }
                                break;
                            }
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to process asset record at index {i}: {ex.Message}");
                }
            }

            result.UpdatedRecords = zoneAssetRecords;
            return result;
        }

        /// <summary>
        /// Updates the zone asset record with the data from T (where T is an IAssetRecordUpdatable)
        /// and sets an AdditionalData string for debugging.
        /// </summary>
        private static void UpdateAssetRecord<T>(List<ZoneAssetRecord> zoneAssetRecords, int index, T record, string assetRecordMethod) where T : IAssetRecordUpdatable
        {
            var assetRecord = zoneAssetRecords[index];
            record.UpdateAssetRecord(ref assetRecord);
            assetRecord.AdditionalData = assetRecordMethod;
            zoneAssetRecords[index] = assetRecord;
            Debug.WriteLine($"Updated asset record at index {index}: {assetRecord.Name}");
        }
    }
}
