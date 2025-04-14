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
        /// (such as RawFileNode, StringTable, and Localize) and returns them in a ZoneAssetRecords object.
        /// </summary>
        /// <param name="openedFastFile">The FastFile object containing the zone data.</param>
        /// <param name="zoneAssetRecords">The list of asset records extracted from the zone.</param>
        /// <returns>A ZoneAssetRecords object containing updated asset lists and records.</returns>
        public static ZoneAssetRecords ProcessAssetRecords(FastFile openedFastFile, List<ZoneAssetRecord> zoneAssetRecords)
        {
            // Create the result container.
            ZoneAssetRecords result = new ZoneAssetRecords();

            // Keep track of the index and end offset of the last successfully parsed asset record.
            int indexOfLastAssetRecordParsed = 0;
            int previousRecordEndOffset = 0;
            string assetRecordMethod = string.Empty;

            // Loop through each asset record.
            for (int i = 0; i < zoneAssetRecords.Count; i++)
            {
                try
                {
                    // For all records except the first, update previousRecordEndOffset.
                    if (i > 0)
                        previousRecordEndOffset = zoneAssetRecords[i - 1].AssetRecordEndOffset;

                    Debug.WriteLine($"Processing record index {i}, previousRecordEndOffset: {previousRecordEndOffset}");

                    // Determine the starting offset for the current record:
                    // - For the first record, use the AssetPoolEndOffset.
                    // - For subsequent records, if previousRecordEndOffset is available, use it;
                    //   otherwise, fall back to the last parsed record's end offset.
                    int startingOffset = (i == 0)
                        ? openedFastFile.OpenedFastFileZone.AssetPoolEndOffset
                        : (previousRecordEndOffset > 0
                            ? previousRecordEndOffset
                            : zoneAssetRecords[indexOfLastAssetRecordParsed].AssetRecordEndOffset);

                    // Process the record based on its type.
                    switch (zoneAssetRecords[i].AssetType)
                    {
                        case ZoneFileAssetType_COD5.rawfile:
                            {
                                // Try to extract a raw file using the no-pattern method first.
                                RawFileNode node = RawFileParser.ExtractSingleRawFileNodeNoPattern(openedFastFile, startingOffset)
                                    // If no-pattern extraction fails, fall back to pattern matching.
                                    ?? RawFileParser.ExtractSingleRawFileNodeWithPattern(openedFastFile.ZoneFilePath, startingOffset);

                                if (node != null)
                                {
                                    // Set the extraction method description based on the offset used.
                                    assetRecordMethod = (previousRecordEndOffset > 0)
                                        ? "Raw file parsed using previous record's offset."
                                        : "Raw file parsed from asset pool end using pattern matching.";
                                    result.RawFileNodes.Add(node);
                                    UpdateAssetRecord(zoneAssetRecords, i, node, assetRecordMethod);
                                    indexOfLastAssetRecordParsed = i;
                                }
                                break;
                            }
                        case ZoneFileAssetType_COD5.stringtable:
                            {
                                // Parse a string table using a similar conditional approach.
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
                        case ZoneFileAssetType_COD5.localize:
                            {
                                // Use a ternary operator to choose between the no-pattern and pattern methods.
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
                            // If asset type is not handled, do nothing.
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to process asset record at index {i}: {ex.Message}");
                }
            }

            // Save the updated asset records into the result container.
            result.UpdatedRecords = zoneAssetRecords;
            return result;
        }

        /// <summary>
        /// Updates the zone asset record at the specified index with data from an asset that implements IAssetRecordUpdatable.
        /// Also sets an AdditionalData string for debugging purposes.
        /// </summary>
        /// <typeparam name="T">Type implementing IAssetRecordUpdatable.</typeparam>
        /// <param name="zoneAssetRecords">The list of asset records.</param>
        /// <param name="index">Index of the record to update.</param>
        /// <param name="record">Asset data used for updating.</param>
        /// <param name="assetRecordMethod">A descriptive message of the extraction method used.</param>
        private static void UpdateAssetRecord<T>(List<ZoneAssetRecord> zoneAssetRecords, int index, T record, string assetRecordMethod) where T : IAssetRecordUpdatable
        {
            // Retrieve the asset record at the given index.
            var assetRecord = zoneAssetRecords[index];
            // Update the asset record using the provided data.
            record.UpdateAssetRecord(ref assetRecord);
            // Store the extraction method message for debugging.
            assetRecord.AdditionalData = assetRecordMethod;
            // Write the updated asset record back into the list.
            zoneAssetRecords[index] = assetRecord;
            Debug.WriteLine($"Updated asset record at index {index}: {assetRecord.Name}");
        }
    }
}
