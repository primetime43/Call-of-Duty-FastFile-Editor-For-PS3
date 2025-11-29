using Call_of_Duty_FastFile_Editor.GameDefinitions;
using Call_of_Duty_FastFile_Editor.Models;
using Call_of_Duty_FastFile_Editor.ZoneParsers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Call_of_Duty_FastFile_Editor.Services
{
    public static class AssetRecordProcessor
    {
        /// <summary>
        /// Checks if a COD4 asset type is supported for structure-based parsing.
        /// </summary>
        private static bool IsSupportedAssetType_COD4(CoD4AssetType assetType)
        {
            return assetType == CoD4AssetType.rawfile ||
                   assetType == CoD4AssetType.localize;
        }

        /// <summary>
        /// Checks if a COD5 asset type is supported for structure-based parsing.
        /// </summary>
        private static bool IsSupportedAssetType_COD5(CoD5AssetType assetType)
        {
            return assetType == CoD5AssetType.rawfile ||
                   assetType == CoD5AssetType.localize;
        }

        /// <summary>
        /// Checks if a MW2 asset type is supported for structure-based parsing.
        /// </summary>
        private static bool IsSupportedAssetType_MW2(MW2AssetType assetType)
        {
            return assetType == MW2AssetType.rawfile ||
                   assetType == MW2AssetType.localize;
        }

        /// <summary>
        /// Processes the given zone asset records, extracting various asset types
        /// and returns them in a ZoneAssetRecords object.
        /// Uses structure-based parsing first, then falls back to pattern matching
        /// for rawfiles that appear after unsupported asset types.
        /// </summary>
        /// <param name="openedFastFile">The FastFile object containing the zone data.</param>
        /// <param name="zoneAssetRecords">The list of asset records extracted from the zone.</param>
        /// <param name="forcePatternMatching">If true, skip structure-based parsing and use pattern matching only.</param>
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
            int structureParsingStoppedAtIndex = -1;
            int lastStructureParsedEndOffset = 0;

            // Zone file data is accessed via openedFastFile.OpenedFastFileZone.Data

            // If forcePatternMatching is true, skip structure-based parsing entirely
            if (forcePatternMatching)
            {
                structureParsingStoppedAtIndex = 0;
                goto PatternMatchingFallback;
            }

            // Loop through each asset record.
            for (int i = 0; i < zoneAssetRecords.Count; i++)
            {
                try
                {
                    // Check if this asset type is supported before processing
                    bool isSupported = false;
                    string assetTypeName = "unknown";

                    if (openedFastFile.IsCod4File)
                    {
                        isSupported = IsSupportedAssetType_COD4(zoneAssetRecords[i].AssetType_COD4);
                        assetTypeName = zoneAssetRecords[i].AssetType_COD4.ToString();
                    }
                    else if (openedFastFile.IsCod5File)
                    {
                        isSupported = IsSupportedAssetType_COD5(zoneAssetRecords[i].AssetType_COD5);
                        assetTypeName = zoneAssetRecords[i].AssetType_COD5.ToString();
                    }
                    else if (openedFastFile.IsMW2File)
                    {
                        isSupported = IsSupportedAssetType_MW2(zoneAssetRecords[i].AssetType_MW2);
                        assetTypeName = zoneAssetRecords[i].AssetType_MW2.ToString();
                    }

                    if (!isSupported)
                    {
                        // Stop structure-based processing - we can't determine the size of unsupported assets
                        // Record where we stopped so we can use pattern matching to find remaining rawfiles
                        Debug.WriteLine($"[AssetRecordProcessor] Stopping structure-based parsing at index {i}: unsupported asset type '{assetTypeName}'. Will use pattern matching for remaining rawfiles.");
                        structureParsingStoppedAtIndex = i;
                        if (i > 0 && zoneAssetRecords[i - 1].AssetRecordEndOffset > 0)
                        {
                            lastStructureParsedEndOffset = zoneAssetRecords[i - 1].AssetRecordEndOffset;
                        }
                        goto PatternMatchingFallback;
                    }

                    // For all records except the first, update previousRecordEndOffset.
                    if (i > 0)
                        previousRecordEndOffset = zoneAssetRecords[i - 1].AssetRecordEndOffset;

                    Debug.WriteLine($"Processing record index {i} ({assetTypeName}), previousRecordEndOffset: {previousRecordEndOffset}");

                    // Determine the starting offset for the current record:
                    // - For the first record, use the AssetPoolEndOffset.
                    // - For subsequent records, use the previous record's end offset.
                    int startingOffset;
                    if (i == 0)
                    {
                        startingOffset = openedFastFile.OpenedFastFileZone.AssetPoolEndOffset;
                    }
                    else if (previousRecordEndOffset > 0)
                    {
                        startingOffset = previousRecordEndOffset;
                    }
                    else
                    {
                        startingOffset = zoneAssetRecords[indexOfLastAssetRecordParsed].AssetRecordEndOffset;
                    }

                    if (openedFastFile.IsCod4File)
                    {
                        // Process the record based on its type.
                        switch (zoneAssetRecords[i].AssetType_COD4)
                        {
                            case CoD4AssetType.rawfile:
                                {
                                    // Structure-based parsing only
                                    RawFileNode node = RawFileParser.ExtractSingleRawFileNodeNoPattern(openedFastFile, startingOffset);

                                    if (node != null)
                                    {
                                        assetRecordMethod = "Raw file parsed using structure-based offset.";
                                        result.RawFileNodes.Add(node);
                                        UpdateAssetRecord(zoneAssetRecords, i, node, assetRecordMethod);
                                        indexOfLastAssetRecordParsed = i;
                                    }
                                    else
                                    {
                                        Debug.WriteLine($"[AssetRecordProcessor] Failed to parse rawfile at index {i}, offset 0x{startingOffset:X}. Stopping.");
                                        goto EndProcessing; // Stop on parse failure
                                    }
                                    break;
                                }
                            case CoD4AssetType.localize:
                                {
                                    // Always use structure-based parsing
                                    var tuple = LocalizeAssetParser.ParseSingleLocalizeAssetNoPattern(openedFastFile, startingOffset);
                                    LocalizedEntry localizedEntry = tuple.entry;

                                    if (localizedEntry != null)
                                    {
                                        assetRecordMethod = "Localized asset parsed using structure-based offset.";
                                        result.LocalizedEntries.Add(localizedEntry);
                                        UpdateAssetRecord(zoneAssetRecords, i, localizedEntry, assetRecordMethod);
                                        indexOfLastAssetRecordParsed = i;
                                    }
                                    else
                                    {
                                        Debug.WriteLine($"[AssetRecordProcessor] Failed to parse localize at index {i}, offset 0x{startingOffset:X}. Stopping.");
                                        goto EndProcessing; // Stop on parse failure
                                    }
                                    break;
                                }
                        }
                    }
                    else if (openedFastFile.IsCod5File)
                    {
                        // Process the record based on its type.
                        switch (zoneAssetRecords[i].AssetType_COD5)
                        {
                            case CoD5AssetType.rawfile:
                                {
                                    // Structure-based parsing only
                                    RawFileNode node = RawFileParser.ExtractSingleRawFileNodeNoPattern(openedFastFile, startingOffset);

                                    if (node != null)
                                    {
                                        assetRecordMethod = "Raw file parsed using structure-based offset.";
                                        result.RawFileNodes.Add(node);
                                        UpdateAssetRecord(zoneAssetRecords, i, node, assetRecordMethod);
                                        indexOfLastAssetRecordParsed = i;
                                    }
                                    else
                                    {
                                        Debug.WriteLine($"[AssetRecordProcessor] Failed to parse rawfile at index {i}, offset 0x{startingOffset:X}. Stopping.");
                                        goto EndProcessing; // Stop on parse failure
                                    }
                                    break;
                                }
                            case CoD5AssetType.localize:
                                {
                                    // Always use structure-based parsing
                                    var tuple = LocalizeAssetParser.ParseSingleLocalizeAssetNoPattern(openedFastFile, startingOffset);
                                    LocalizedEntry localizedEntry = tuple.entry;

                                    if (localizedEntry != null)
                                    {
                                        assetRecordMethod = "Localized asset parsed using structure-based offset.";
                                        result.LocalizedEntries.Add(localizedEntry);
                                        UpdateAssetRecord(zoneAssetRecords, i, localizedEntry, assetRecordMethod);
                                        indexOfLastAssetRecordParsed = i;
                                    }
                                    else
                                    {
                                        Debug.WriteLine($"[AssetRecordProcessor] Failed to parse localize at index {i}, offset 0x{startingOffset:X}. Stopping.");
                                        goto EndProcessing; // Stop on parse failure
                                    }
                                    break;
                                }
                        }
                    }
                    else if (openedFastFile.IsMW2File)
                    {
                        // Process the record based on its type.
                        switch (zoneAssetRecords[i].AssetType_MW2)
                        {
                            case MW2AssetType.rawfile:
                                {
                                    // Structure-based parsing only
                                    RawFileNode node = RawFileParser.ExtractSingleRawFileNodeNoPattern(openedFastFile, startingOffset);

                                    if (node != null)
                                    {
                                        assetRecordMethod = "Raw file parsed using structure-based offset.";
                                        result.RawFileNodes.Add(node);
                                        UpdateAssetRecord(zoneAssetRecords, i, node, assetRecordMethod);
                                        indexOfLastAssetRecordParsed = i;
                                    }
                                    else
                                    {
                                        Debug.WriteLine($"[AssetRecordProcessor] Failed to parse rawfile at index {i}, offset 0x{startingOffset:X}. Stopping.");
                                        goto EndProcessing; // Stop on parse failure
                                    }
                                    break;
                                }
                            case MW2AssetType.localize:
                                {
                                    // Always use structure-based parsing
                                    var tuple = LocalizeAssetParser.ParseSingleLocalizeAssetNoPattern(openedFastFile, startingOffset);
                                    LocalizedEntry localizedEntry = tuple.entry;

                                    if (localizedEntry != null)
                                    {
                                        assetRecordMethod = "Localized asset parsed using structure-based offset.";
                                        result.LocalizedEntries.Add(localizedEntry);
                                        UpdateAssetRecord(zoneAssetRecords, i, localizedEntry, assetRecordMethod);
                                        indexOfLastAssetRecordParsed = i;
                                    }
                                    else
                                    {
                                        Debug.WriteLine($"[AssetRecordProcessor] Failed to parse localize at index {i}, offset 0x{startingOffset:X}. Stopping.");
                                        goto EndProcessing; // Stop on parse failure
                                    }
                                    break;
                                }
                        }
                    }

                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to process asset record at index {i}: {ex.Message}. Stopping.");
                    break; // Stop on exception
                }
            }

        PatternMatchingFallback:
            // If structure-based parsing stopped early due to unsupported assets,
            // use pattern matching to find remaining rawfiles
            if (structureParsingStoppedAtIndex >= 0)
            {
                Debug.WriteLine($"[AssetRecordProcessor] Starting pattern matching fallback from offset 0x{lastStructureParsedEndOffset:X}");

                byte[] zoneData = openedFastFile.OpenedFastFileZone.Data;
                int searchStartOffset = lastStructureParsedEndOffset > 0
                    ? lastStructureParsedEndOffset
                    : openedFastFile.OpenedFastFileZone.AssetPoolEndOffset;

                // Get already found rawfile names to avoid duplicates
                var existingFileNames = new HashSet<string>(
                    result.RawFileNodes.Select(n => n.FileName),
                    StringComparer.OrdinalIgnoreCase);

                // Use pattern matching to scan for rawfiles
                int currentOffset = searchStartOffset;
                int patternMatchedCount = 0;

                while (currentOffset < zoneData.Length)
                {
                    RawFileNode node = RawFileParser.ExtractSingleRawFileNodeWithPattern(zoneData, currentOffset);

                    if (node == null)
                    {
                        // No more rawfiles found
                        break;
                    }

                    // Check if we already have this file from structure-based parsing
                    if (!existingFileNames.Contains(node.FileName))
                    {
                        node.AdditionalData = "Raw file parsed using pattern matching (fallback).";
                        result.RawFileNodes.Add(node);
                        existingFileNames.Add(node.FileName);
                        patternMatchedCount++;
                        Debug.WriteLine($"[AssetRecordProcessor] Pattern matched rawfile: '{node.FileName}' at offset 0x{node.StartOfFileHeader:X}");
                    }

                    // Move past this file to continue searching
                    currentOffset = node.RawFileEndPosition;
                }

                Debug.WriteLine($"[AssetRecordProcessor] Pattern matching found {patternMatchedCount} additional rawfiles");
            }

            EndProcessing:
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
