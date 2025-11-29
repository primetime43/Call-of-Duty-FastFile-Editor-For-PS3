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
        /// Processes the given zone asset records, extracting various asset types
        /// and returns them in a ZoneAssetRecords object.
        /// Uses the game-specific parser from IGameDefinition for structure-based parsing,
        /// then falls back to pattern matching for rawfiles that appear after unsupported asset types.
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
            // Get the appropriate game definition for this FastFile
            IGameDefinition gameDefinition = GameDefinitionFactory.GetDefinition(openedFastFile);
            Debug.WriteLine($"[AssetRecordProcessor] Using game definition: {gameDefinition.ShortName}");

            // Create the result container.
            AssetRecordCollection result = new AssetRecordCollection();

            // Keep track of the index and end offset of the last successfully parsed asset record.
            int indexOfLastAssetRecordParsed = 0;
            int previousRecordEndOffset = 0;
            string assetRecordMethod = string.Empty;
            int structureParsingStoppedAtIndex = -1;
            int lastStructureParsedEndOffset = 0;

            // Zone file data
            byte[] zoneData = openedFastFile.OpenedFastFileZone.Data;

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
                    // Get the asset type value based on game
                    int assetTypeValue = GetAssetTypeValue(openedFastFile, zoneAssetRecords[i]);
                    string assetTypeName = gameDefinition.GetAssetTypeName(assetTypeValue);

                    // Check if this asset type is supported
                    bool isSupported = gameDefinition.IsSupportedAssetType(assetTypeValue);

                    if (!isSupported)
                    {
                        // Stop structure-based processing - we can't determine the size of unsupported assets
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

                    // Determine the starting offset for the current record
                    int startingOffset = DetermineStartingOffset(openedFastFile, zoneAssetRecords, i, previousRecordEndOffset, indexOfLastAssetRecordParsed);

                    // Use game-specific parser based on asset type
                    if (gameDefinition.IsRawFileType(assetTypeValue))
                    {
                        // Parse rawfile using game-specific parser
                        RawFileNode? node = gameDefinition.ParseRawFile(zoneData, startingOffset);

                        if (node != null)
                        {
                            assetRecordMethod = $"Raw file parsed using {gameDefinition.ShortName} structure-based parser.";
                            result.RawFileNodes.Add(node);
                            UpdateAssetRecord(zoneAssetRecords, i, node, assetRecordMethod);
                            indexOfLastAssetRecordParsed = i;
                        }
                        else
                        {
                            Debug.WriteLine($"[AssetRecordProcessor] Failed to parse rawfile at index {i}, offset 0x{startingOffset:X}. Stopping.");
                            structureParsingStoppedAtIndex = i;
                            lastStructureParsedEndOffset = startingOffset;
                            goto PatternMatchingFallback;
                        }
                    }
                    else if (gameDefinition.IsLocalizeType(assetTypeValue))
                    {
                        // Parse localize using game-specific parser
                        var (entry, nextOffset) = gameDefinition.ParseLocalizedEntry(zoneData, startingOffset);

                        if (entry != null)
                        {
                            assetRecordMethod = $"Localized asset parsed using {gameDefinition.ShortName} structure-based parser.";
                            result.LocalizedEntries.Add(entry);

                            // Update the asset record with localize info
                            var assetRecord = zoneAssetRecords[i];
                            assetRecord.AssetRecordEndOffset = nextOffset;
                            assetRecord.Name = entry.Key;
                            assetRecord.Content = entry.LocalizedText;
                            assetRecord.AdditionalData = assetRecordMethod;
                            zoneAssetRecords[i] = assetRecord;

                            indexOfLastAssetRecordParsed = i;
                        }
                        else
                        {
                            Debug.WriteLine($"[AssetRecordProcessor] Failed to parse localize at index {i}, offset 0x{startingOffset:X}. Stopping.");
                            goto EndProcessing;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to process asset record at index {i}: {ex.Message}. Stopping.");
                    break;
                }
            }

        PatternMatchingFallback:
            // If structure-based parsing stopped early due to unsupported assets,
            // use pattern matching to find remaining rawfiles
            if (structureParsingStoppedAtIndex >= 0)
            {
                Debug.WriteLine($"[AssetRecordProcessor] Starting pattern matching fallback from offset 0x{lastStructureParsedEndOffset:X}");

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

        /// <summary>
        /// Gets the asset type value from the record based on the game type.
        /// </summary>
        private static int GetAssetTypeValue(FastFile fastFile, ZoneAssetRecord record)
        {
            if (fastFile.IsCod4File)
                return (int)record.AssetType_COD4;
            if (fastFile.IsCod5File)
                return (int)record.AssetType_COD5;
            if (fastFile.IsMW2File)
                return (int)record.AssetType_MW2;
            return 0;
        }

        /// <summary>
        /// Determines the starting offset for parsing an asset record.
        /// </summary>
        private static int DetermineStartingOffset(
            FastFile fastFile,
            List<ZoneAssetRecord> records,
            int currentIndex,
            int previousRecordEndOffset,
            int indexOfLastParsed)
        {
            if (currentIndex == 0)
            {
                return fastFile.OpenedFastFileZone.AssetPoolEndOffset;
            }
            else if (previousRecordEndOffset > 0)
            {
                return previousRecordEndOffset;
            }
            else
            {
                return records[indexOfLastParsed].AssetRecordEndOffset;
            }
        }
    }
}
