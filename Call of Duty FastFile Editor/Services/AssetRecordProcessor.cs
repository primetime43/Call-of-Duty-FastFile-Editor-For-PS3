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

            // Log the starting offset for debugging
            int assetPoolEndOffset = openedFastFile.OpenedFastFileZone.AssetPoolEndOffset;
            Debug.WriteLine($"[AssetRecordProcessor] AssetPoolEndOffset = 0x{assetPoolEndOffset:X}");
            if (assetPoolEndOffset + 16 <= zoneData.Length)
            {
                string first16Bytes = BitConverter.ToString(zoneData, assetPoolEndOffset, 16).Replace("-", " ");
                Debug.WriteLine($"[AssetRecordProcessor] First 16 bytes at AssetPoolEndOffset: {first16Bytes}");
            }

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
                            // Localize parsing failed - fall back to pattern matching for remaining rawfiles
                            Debug.WriteLine($"[AssetRecordProcessor] Failed to parse localize at index {i}, offset 0x{startingOffset:X}. Will use pattern matching for remaining rawfiles.");
                            structureParsingStoppedAtIndex = i;
                            lastStructureParsedEndOffset = startingOffset;
                            goto PatternMatchingFallback;
                        }
                    }
                    else if (gameDefinition.IsMenuFileType(assetTypeValue))
                    {
                        // Parse menufile using game-specific parser
                        MenuList? menuList = gameDefinition.ParseMenuFile(zoneData, startingOffset);

                        if (menuList != null)
                        {
                            assetRecordMethod = $"MenuList parsed using {gameDefinition.ShortName} structure-based parser.";
                            result.MenuLists.Add(menuList);

                            // Update the asset record with menufile info
                            var assetRecord = zoneAssetRecords[i];
                            assetRecord.AssetRecordEndOffset = menuList.DataEndOffset;
                            assetRecord.Name = menuList.Name;
                            assetRecord.Content = $"{menuList.MenuCount} menus";
                            assetRecord.AdditionalData = assetRecordMethod;
                            zoneAssetRecords[i] = assetRecord;

                            indexOfLastAssetRecordParsed = i;
                        }
                        else
                        {
                            // MenuFile parsing failed
                            Debug.WriteLine($"[AssetRecordProcessor] Failed to parse menufile at index {i}, offset 0x{startingOffset:X}. Stopping.");
                            structureParsingStoppedAtIndex = i;
                            lastStructureParsedEndOffset = startingOffset;
                            goto PatternMatchingFallback;
                        }
                    }
                    else if (gameDefinition.IsMaterialType(assetTypeValue))
                    {
                        // Parse material using game-specific parser
                        MaterialAsset? material = gameDefinition.ParseMaterial(zoneData, startingOffset);

                        if (material != null)
                        {
                            assetRecordMethod = $"Material parsed using {gameDefinition.ShortName} structure-based parser.";
                            result.Materials.Add(material);

                            // Update the asset record with material info
                            var assetRecord = zoneAssetRecords[i];
                            assetRecord.AssetRecordEndOffset = material.EndOffset;
                            assetRecord.Name = material.Name;
                            assetRecord.Content = $"Material: {material.Name}";
                            assetRecord.AdditionalData = assetRecordMethod;
                            zoneAssetRecords[i] = assetRecord;

                            indexOfLastAssetRecordParsed = i;
                        }
                        else
                        {
                            // Material parsing failed - continue to next asset
                            Debug.WriteLine($"[AssetRecordProcessor] Failed to parse material at index {i}, offset 0x{startingOffset:X}. Continuing.");
                            // Don't stop - materials are complex, just skip
                        }
                    }
                    else if (gameDefinition.IsTechSetType(assetTypeValue))
                    {
                        // Parse techset using game-specific parser
                        TechSetAsset? techSet = gameDefinition.ParseTechSet(zoneData, startingOffset);

                        if (techSet != null)
                        {
                            assetRecordMethod = $"TechSet parsed using {gameDefinition.ShortName} structure-based parser.";
                            result.TechSets.Add(techSet);

                            // Update the asset record with techset info
                            var assetRecord = zoneAssetRecords[i];
                            assetRecord.AssetRecordEndOffset = techSet.EndOffset;
                            assetRecord.Name = techSet.Name;
                            assetRecord.Content = $"TechSet: {techSet.Name}";
                            assetRecord.AdditionalData = assetRecordMethod;
                            zoneAssetRecords[i] = assetRecord;

                            indexOfLastAssetRecordParsed = i;
                        }
                        else
                        {
                            // TechSet parsing failed - continue to next asset
                            Debug.WriteLine($"[AssetRecordProcessor] Failed to parse techset at index {i}, offset 0x{startingOffset:X}. Continuing.");
                            // Don't stop - techsets are complex, just skip
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

                // Count expected rawfiles from the asset pool
                int expectedRawFileCount = CountExpectedAssetType(openedFastFile, zoneAssetRecords,
                    structureParsingStoppedAtIndex, gameDefinition.RawFileAssetType);
                int alreadyParsedRawFiles = result.RawFileNodes.Count;
                int remainingRawFiles = expectedRawFileCount - alreadyParsedRawFiles;

                Debug.WriteLine($"[AssetRecordProcessor] Expected {expectedRawFileCount} rawfiles, already parsed {alreadyParsedRawFiles}, remaining {remainingRawFiles}");

                int currentOffset = searchStartOffset;
                int rawFilesParsed = 0;
                bool needPatternMatchForFirst = true;

                while (rawFilesParsed < remainingRawFiles && currentOffset < zoneData.Length)
                {
                    RawFileNode? node = null;
                    string parseMethod = "";

                    // If we have a known offset (from previous file), try structure-based parsing first
                    if (!needPatternMatchForFirst)
                    {
                        node = gameDefinition.ParseRawFile(zoneData, currentOffset);
                        if (node != null)
                        {
                            parseMethod = $"Raw file parsed using {gameDefinition.ShortName} structure-based parser (fallback sequential).";
                        }
                    }

                    // If structure-based parsing failed or we need to find the first one, use pattern matching
                    if (node == null)
                    {
                        node = RawFileParser.ExtractSingleRawFileNodeWithPattern(zoneData, currentOffset, gameDefinition);
                        if (node != null)
                        {
                            parseMethod = "Raw file parsed using pattern matching (fallback).";
                        }
                    }

                    if (node == null)
                    {
                        // No more rawfiles found
                        Debug.WriteLine($"[AssetRecordProcessor] No rawfile found at offset 0x{currentOffset:X}, stopping");
                        break;
                    }

                    // Check if we already have this file from structure-based parsing
                    if (!existingFileNames.Contains(node.FileName))
                    {
                        node.AdditionalData = parseMethod;
                        result.RawFileNodes.Add(node);
                        existingFileNames.Add(node.FileName);
                        rawFilesParsed++;
                        Debug.WriteLine($"[AssetRecordProcessor] Fallback parsed rawfile #{rawFilesParsed}: '{node.FileName}' at offset 0x{node.StartOfFileHeader:X}");
                    }

                    // Move past this file - next iteration can try structure-based parsing
                    currentOffset = node.RawFileEndPosition;
                    needPatternMatchForFirst = false; // We now have a known offset for next file
                }

                Debug.WriteLine($"[AssetRecordProcessor] Fallback found {rawFilesParsed} additional rawfiles");

                // For localized entries, use the asset pool to know exactly how many to expect
                // Then parse them sequentially (NOT pattern scanning the entire zone)
                int expectedLocalizeCount = CountExpectedAssetType(openedFastFile, zoneAssetRecords,
                    structureParsingStoppedAtIndex, gameDefinition.LocalizeAssetType);
                int alreadyParsedLocalizes = result.LocalizedEntries.Count;
                int remainingLocalizes = expectedLocalizeCount - alreadyParsedLocalizes;

                Debug.WriteLine($"[AssetRecordProcessor] Expected {expectedLocalizeCount} localizes, already parsed {alreadyParsedLocalizes}, remaining {remainingLocalizes}");

                if (remainingLocalizes > 0)
                {
                    // Find the first localize marker (limited search, not entire zone)
                    int localizeStartOffset = FindFirstLocalizeMarker(zoneData, searchStartOffset,
                        Math.Min(searchStartOffset + 100000, zoneData.Length)); // Search up to 100KB

                    if (localizeStartOffset >= 0)
                    {
                        Debug.WriteLine($"[AssetRecordProcessor] Found first localize marker at 0x{localizeStartOffset:X}");

                        // Parse localizes sequentially using structure-based parser
                        int localizeCurrentOffset = localizeStartOffset;
                        int localizesParsed = 0;

                        while (localizesParsed < remainingLocalizes && localizeCurrentOffset < zoneData.Length)
                        {
                            var (entry, nextOffset) = gameDefinition.ParseLocalizedEntry(zoneData, localizeCurrentOffset);

                            if (entry == null || nextOffset <= localizeCurrentOffset)
                            {
                                Debug.WriteLine($"[AssetRecordProcessor] Failed to parse localize #{localizesParsed + 1} at 0x{localizeCurrentOffset:X}");
                                break;
                            }

                            result.LocalizedEntries.Add(entry);
                            localizesParsed++;
                            Debug.WriteLine($"[AssetRecordProcessor] Parsed localize #{localizesParsed}: '{entry.Key}' at 0x{localizeCurrentOffset:X}");

                            localizeCurrentOffset = nextOffset;
                        }

                        Debug.WriteLine($"[AssetRecordProcessor] Sequential parsing found {localizesParsed} localized entries");
                    }
                    else
                    {
                        Debug.WriteLine($"[AssetRecordProcessor] Could not find localize marker in search range");
                    }
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

        /// <summary>
        /// Counts the expected number of a specific asset type from the asset pool records.
        /// </summary>
        private static int CountExpectedAssetType(FastFile fastFile, List<ZoneAssetRecord> records, int startIndex, byte assetType)
        {
            int count = 0;
            for (int i = startIndex; i < records.Count; i++)
            {
                int recordAssetType = GetAssetTypeValue(fastFile, records[i]);
                if (recordAssetType == assetType)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Finds the first valid localize marker within the specified range.
        /// A valid marker is either:
        /// - 8 consecutive 0xFF bytes (both value and key inline)
        /// - 4 non-0xFF bytes followed by 4 0xFF bytes (key only inline)
        /// Also validates the key looks like a proper localize key (SCREAMING_SNAKE_CASE).
        /// </summary>
        private static int FindFirstLocalizeMarker(byte[] data, int startOffset, int endOffset)
        {
            for (int pos = startOffset; pos <= endOffset - 8; pos++)
            {
                // Check if bytes 4-7 are FF (key pointer must be inline)
                if (data[pos + 4] != 0xFF || data[pos + 5] != 0xFF ||
                    data[pos + 6] != 0xFF || data[pos + 7] != 0xFF)
                {
                    continue;
                }

                // Check if first 4 bytes are also FF (both inline) or not
                bool valuePointerIsFF = data[pos] == 0xFF && data[pos + 1] == 0xFF &&
                                        data[pos + 2] == 0xFF && data[pos + 3] == 0xFF;

                // Verify there's valid data after the marker
                if (pos + 8 >= data.Length)
                    continue;

                byte nextByte = data[pos + 8];

                // Skip if still in padding (next byte is FF)
                if (nextByte == 0xFF)
                    continue;

                // If key-only (value not inline), next byte should be printable (start of key)
                if (!valuePointerIsFF && nextByte == 0x00)
                    continue;

                // Additional validation: try to read the key and check it looks like a localize key
                int keyOffset = pos + 8;
                if (valuePointerIsFF)
                {
                    // Skip the value string first to get to the key
                    while (keyOffset < data.Length && data[keyOffset] != 0x00)
                        keyOffset++;
                    keyOffset++; // Skip null terminator
                }

                // Read the key
                string key = ReadNullTerminatedStringAt(data, keyOffset);
                if (!IsValidLocalizeKey(key))
                {
                    continue; // Not a valid localize key, keep searching
                }

                // This looks like a valid localize marker
                return pos;
            }

            return -1; // Not found
        }

        /// <summary>
        /// Reads a null-terminated string from the data at the given offset.
        /// </summary>
        private static string ReadNullTerminatedStringAt(byte[] data, int offset)
        {
            var sb = new System.Text.StringBuilder();
            while (offset < data.Length && data[offset] != 0x00)
            {
                sb.Append((char)data[offset]);
                offset++;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Validates that a string looks like a valid localization key.
        /// Valid keys are in SCREAMING_SNAKE_CASE format.
        /// </summary>
        private static bool IsValidLocalizeKey(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length < 2 || key.Length > 100)
                return false;

            // Must start with an uppercase letter
            if (!char.IsUpper(key[0]))
                return false;

            // Check all characters are valid (uppercase letters, digits, underscores)
            foreach (char c in key)
            {
                if (!char.IsUpper(c) && !char.IsDigit(c) && c != '_')
                    return false;
            }

            // Must contain at least one underscore
            if (!key.Contains('_'))
                return false;

            return true;
        }
    }
}
