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
        /// and returns them in a ZoneAssetRecords object.
        /// </summary>
        /// <param name="openedFastFile">The FastFile object containing the zone data.</param>
        /// <param name="zoneAssetRecords">The list of asset records extracted from the zone.</param>
        /// <param name="forcePatternMatching">
        /// If true, raw files will be parsed using pattern matching, which overrides the default structured parsing.
        /// Use this option when the standard parsing does not correctly detect raw files.
        /// </param>
        /// <returns>A ZoneAssetRecords object containing updated asset lists and records.</returns>
        public static AssetRecordCollection ProcessAssetRecords(
            FastFile openedFastFile,
            List<ZoneAssetRecord> zoneAssetRecords,
            bool forcePatternMatching = false
        )
        {
            // Create the result container.
            AssetRecordCollection result = new AssetRecordCollection();

            // Keep track of the index and end offset of the last successfully parsed asset record.
            int indexOfLastAssetRecordParsed = 0;
            int previousRecordEndOffset = 0;
            string assetRecordMethod = string.Empty;

            // Flag to track if we have a known good starting offset
            // For i=0, we use AssetPoolEndOffset which IS a known good offset
            bool hasKnownStartingOffset = false;

            // Load file data ONCE for pattern matching
            byte[] zoneFileData = System.IO.File.ReadAllBytes(openedFastFile.ZoneFilePath);

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
                    int startingOffset;
                    if (i == 0)
                    {
                        startingOffset = openedFastFile.OpenedFastFileZone.AssetPoolEndOffset;
                        hasKnownStartingOffset = true; // We know AssetPoolEndOffset from structure parsing
                    }
                    else if (previousRecordEndOffset > 0)
                    {
                        startingOffset = previousRecordEndOffset;
                        hasKnownStartingOffset = true;
                    }
                    else
                    {
                        startingOffset = zoneAssetRecords[indexOfLastAssetRecordParsed].AssetRecordEndOffset;
                        hasKnownStartingOffset = startingOffset > 0;
                    }

                    if (openedFastFile.IsCod4File)
                    {
                        // Process the record based on its type.
                        switch (zoneAssetRecords[i].AssetType_COD4)
                        {
                            case ZoneFileAssetType_COD4.rawfile:
                                {
                                    RawFileNode node = forcePatternMatching
                                    ? RawFileParser.ExtractSingleRawFileNodeWithPattern(zoneFileData, startingOffset)
                                    : RawFileParser.ExtractSingleRawFileNodeNoPattern(openedFastFile, startingOffset)
                                        ?? RawFileParser.ExtractSingleRawFileNodeWithPattern(zoneFileData, startingOffset);

                                    if (node != null)
                                    {
                                        // Set the extraction method description based on the offset used.
                                        assetRecordMethod = hasKnownStartingOffset
                                            ? "Raw file parsed using structure-based offset."
                                            : "Raw file parsed using pattern matching.";
                                        result.RawFileNodes.Add(node);
                                        UpdateAssetRecord(zoneAssetRecords, i, node, assetRecordMethod);
                                        indexOfLastAssetRecordParsed = i;
                                    }
                                    break;
                                }
                            case ZoneFileAssetType_COD4.stringtable:
                                {
                                    StringTable table = forcePatternMatching
                                    ? StringTable.FindSingleCsvStringTableWithPattern(openedFastFile.OpenedFastFileZone, startingOffset)
                                    : (hasKnownStartingOffset
                                        ? StringTableParser.ParseStringTable(openedFastFile, startingOffset)
                                        : StringTable.FindSingleCsvStringTableWithPattern(openedFastFile.OpenedFastFileZone, startingOffset));


                                    if (table != null)
                                    {
                                        assetRecordMethod = hasKnownStartingOffset
                                            ? "String table parsed using structure-based offset."
                                            : "String table parsed using pattern matching.";
                                        result.StringTables.Add(table);
                                        UpdateAssetRecord(zoneAssetRecords, i, table, assetRecordMethod);
                                        indexOfLastAssetRecordParsed = i;
                                    }
                                    break;
                                }
                            case ZoneFileAssetType_COD4.localize:
                                {
                                    var tuple = forcePatternMatching
                                    ? LocalizeAssetParser.ParseSingleLocalizeAssetWithPattern(openedFastFile, startingOffset)
                                    : (hasKnownStartingOffset
                                        ? LocalizeAssetParser.ParseSingleLocalizeAssetNoPattern(openedFastFile, startingOffset)
                                        : LocalizeAssetParser.ParseSingleLocalizeAssetWithPattern(openedFastFile, startingOffset));

                                    LocalizedEntry localizedEntry = tuple.entry;
                                    assetRecordMethod = hasKnownStartingOffset
                                        ? "Localized asset parsed using structure-based offset."
                                        : "Localized asset parsed using pattern matching.";

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
                    else if (openedFastFile.IsCod5File)
                    {
                        // Process the record based on its type.
                        switch (zoneAssetRecords[i].AssetType_COD5)
                        {
                            case ZoneFileAssetType_COD5.rawfile:
                                {
                                    RawFileNode node = forcePatternMatching
                                    ? RawFileParser.ExtractSingleRawFileNodeWithPattern(zoneFileData, startingOffset)
                                    : RawFileParser.ExtractSingleRawFileNodeNoPattern(openedFastFile, startingOffset)
                                        ?? RawFileParser.ExtractSingleRawFileNodeWithPattern(zoneFileData, startingOffset);

                                    if (node != null)
                                    {
                                        // Set the extraction method description based on the offset used.
                                        assetRecordMethod = hasKnownStartingOffset
                                            ? "Raw file parsed using structure-based offset."
                                            : "Raw file parsed using pattern matching.";
                                        result.RawFileNodes.Add(node);
                                        UpdateAssetRecord(zoneAssetRecords, i, node, assetRecordMethod);
                                        indexOfLastAssetRecordParsed = i;
                                    }
                                    break;
                                }
                            case ZoneFileAssetType_COD5.stringtable:
                                {
                                    StringTable table = forcePatternMatching
                                    ? StringTable.FindSingleCsvStringTableWithPattern(openedFastFile.OpenedFastFileZone, startingOffset)
                                    : (hasKnownStartingOffset
                                        ? StringTableParser.ParseStringTable(openedFastFile, startingOffset)
                                        : StringTable.FindSingleCsvStringTableWithPattern(openedFastFile.OpenedFastFileZone, startingOffset));

                                    if (table != null)
                                    {
                                        assetRecordMethod = hasKnownStartingOffset
                                            ? "String table parsed using structure-based offset."
                                            : "String table parsed using pattern matching.";
                                        result.StringTables.Add(table);
                                        UpdateAssetRecord(zoneAssetRecords, i, table, assetRecordMethod);
                                        indexOfLastAssetRecordParsed = i;
                                    }
                                    break;
                                }
                            case ZoneFileAssetType_COD5.localize:
                                {
                                    var tuple = forcePatternMatching
                                    ? LocalizeAssetParser.ParseSingleLocalizeAssetWithPattern(openedFastFile, startingOffset)
                                    : (hasKnownStartingOffset
                                        ? LocalizeAssetParser.ParseSingleLocalizeAssetNoPattern(openedFastFile, startingOffset)
                                        : LocalizeAssetParser.ParseSingleLocalizeAssetWithPattern(openedFastFile, startingOffset));


                                    LocalizedEntry localizedEntry = tuple.entry;
                                    assetRecordMethod = hasKnownStartingOffset
                                        ? "Localized asset parsed using structure-based offset."
                                        : "Localized asset parsed using pattern matching.";

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
