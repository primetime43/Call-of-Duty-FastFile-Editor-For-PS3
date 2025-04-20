using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Call_of_Duty_FastFile_Editor.Models
{
    /// <summary>
    /// Still using pattern matching as this is a complicated structure to parse.
    /// https://codresearch.dev/index.php/Collision_Map_Asset_(WaW)
    /// This currently gets the text data associated with the collision map, but doesn't parse
    /// the actual map's structure. TODO: Parse the actual map structure.
    /// </summary>
    public class MapEntity
    {
        /// <summary>
        /// The file offset where this entity’s '{' was found.
        /// </summary>
        public int SourceOffset { get; set; }

        /// <summary>
        /// Stores key-value pairs (e.g. "classname" -> "worldspawn").
        /// </summary>
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }

    public static class Collision_Map_Operations
    {
        // Regex pattern for a line like: "key" "value"
        private static readonly Regex LinePattern =
            new Regex(@"^""([^""]+)""\s+""([^""]+)""$", RegexOptions.Compiled);

        /// <summary>
        /// Parses the .map entity data from a known offset. We expect:
        ///   [4 bytes big-endian: length]
        ///   [length bytes of ASCII text containing { ... }]
        /// Returns the parsed MapEntity objects.
        /// </summary>
        /// <param name="zone">The zone whose ZoneFileData we are scanning.</param>
        /// <param name="offset">AssetPoolRecordOffset where the 4-byte big-endian size is located.</param>
        public static List<MapEntity> ParseMapEntsAtOffset(ZoneFile zone, int offset)
        {
            var results = new List<MapEntity>();
            byte[] zoneBytes = zone.Data;
            if (zoneBytes == null)
                return results;

            if (offset < 0 || offset + 4 > zoneBytes.Length)
                return results;

            // 1) Read 4 bytes as big-endian => length
            int length = (zoneBytes[offset] << 24)
                       | (zoneBytes[offset + 1] << 16)
                       | (zoneBytes[offset + 2] << 8)
                       | (zoneBytes[offset + 3]);

            if (length <= 0 || offset + 4 + length > zoneBytes.Length)
                return results;

            // 2) Extract the raw chunk of data
            int startOfMapData = offset + 4;
            byte[] mapDataBytes = new byte[length];
            Buffer.BlockCopy(zoneBytes, startOfMapData, mapDataBytes, 0, length);

            // 3) Parse it with our new offset-aware parser
            //    We pass in baseOffset = startOfMapData so that i=0 in the chunk lines up with offset+4 in the file.
            results = ParseDataMapWithOffsets(mapDataBytes, startOfMapData);

            return results;
        }

        public static int FindCollision_Map_DataOffsetViaFF(ZoneFile zone)
        {
            if (zone?.Data == null)
                return -1;

            byte[] data = zone.Data;
            int fileLength = data.Length;

            // 1) Find large runs of 0xFF (say, >= 32 in a row).
            List<int> ffRuns = FindRunsOfFF(data, minRunLength: 32);
            if (ffRuns.Count == 0)
                return -1; // no runs found

            // 2) For each run offset, scan a window around it
            const int SEARCH_WINDOW = 512; // how many bytes before/after the run to check

            foreach (int runOffset in ffRuns)
            {
                // Define the region we’ll search for the .map header
                // For safety, ensure we don’t go below 0 or above fileLength-4
                int windowStart = Math.Max(0, runOffset - SEARCH_WINDOW);
                int windowEnd = Math.Min(fileLength - 4, runOffset + SEARCH_WINDOW);

                for (int i = windowStart; i <= windowEnd; i++)
                {
                    // Try reading 4 bytes big-endian => length
                    int length = (data[i] << 24)
                               | (data[i + 1] << 16)
                               | (data[i + 2] << 8)
                               | (data[i + 3]);

                    if (length <= 0)
                        continue;

                    // Ensure it fits
                    int mapDataStart = i + 4;
                    int mapDataEnd = mapDataStart + length;
                    if (mapDataEnd > fileLength)
                        continue;

                    // Optional quick check for "{" in the first 256 bytes
                    // (similar to your existing code)
                    bool hasBrace = false;
                    int peekEnd = Math.Min(mapDataEnd, mapDataStart + 256);
                    for (int p = mapDataStart; p < peekEnd; p++)
                    {
                        if (data[p] == '{')
                        {
                            hasBrace = true;
                            break;
                        }
                    }
                    if (!hasBrace)
                        continue;

                    // Now do a full parse
                    var testEntities = ParseMapEntsAtOffset(zone, i);
                    if (testEntities.Count > 0)
                    {
                        // Found a valid .map block
                        return i; // immediate success
                    }
                }
            }

            return -1; // If we exhaust all runs/windows without success
        }

        private static List<MapEntity> ParseDataMapWithOffsets(byte[] mapDataBytes, int baseOffset)
        {
            // We'll parse the raw chunk, looking for '{' or '}' and building lines in-between.
            // This gives us exact control over offsets.
            List<MapEntity> entities = new List<MapEntity>();
            MapEntity currentEntity = null;
            bool insideBraces = false;

            // We'll accumulate text for each "line" until we hit '\r' or '\n'
            // Then we test that line for "key" "value".
            StringBuilder lineBuffer = new StringBuilder();

            int length = mapDataBytes.Length;
            for (int i = 0; i < length; i++)
            {
                byte b = mapDataBytes[i];

                if (b == '{')
                {
                    // Start a new entity right here
                    currentEntity = new MapEntity
                    {
                        SourceOffset = baseOffset + i
                    };
                    insideBraces = true;
                }
                else if (b == '}')
                {
                    // Close out the current entity (if it has properties)
                    if (currentEntity != null && currentEntity.Properties.Count > 0)
                    {
                        entities.Add(currentEntity);
                    }
                    currentEntity = null;
                    insideBraces = false;
                }
                else if (b == '\r' || b == '\n')
                {
                    // We reached the end of a line
                    string line = lineBuffer.ToString().Trim();
                    lineBuffer.Clear();

                    if (insideBraces && currentEntity != null && line.Length > 0)
                    {
                        // Attempt a "key" "value" match
                        var match = LinePattern.Match(line);
                        if (match.Success)
                        {
                            string key = match.Groups[1].Value;
                            string value = match.Groups[2].Value;
                            currentEntity.Properties[key] = value;
                        }
                    }
                }
                else
                {
                    // Just accumulate this character for the current line
                    lineBuffer.Append((char)b);
                }
            }

            // At the end of the chunk, if there's a partial line in the buffer, handle it:
            if (lineBuffer.Length > 0 && insideBraces && currentEntity != null)
            {
                string finalLine = lineBuffer.ToString().Trim();
                if (finalLine.Length > 0)
                {
                    var match = LinePattern.Match(finalLine);
                    if (match.Success)
                    {
                        string key = match.Groups[1].Value;
                        string value = match.Groups[2].Value;
                        currentEntity.Properties[key] = value;
                    }
                }
            }

            return entities;
        }

        private static List<int> FindRunsOfFF(byte[] data, int minRunLength)
        {
            // Return a list of offsets where a run of at least minRunLength 0xFF begins.
            // E.g. if minRunLength=32, we find all places where at least 32 consecutive 0xFF are found.
            var runOffsets = new List<int>();

            int i = 0;
            while (i < data.Length)
            {
                // If current byte is 0xFF, see how long this run goes
                if (data[i] == 0xFF)
                {
                    int runStart = i;
                    int runCount = 1;
                    i++;

                    while (i < data.Length && data[i] == 0xFF)
                    {
                        runCount++;
                        i++;
                    }

                    if (runCount >= minRunLength)
                    {
                        runOffsets.Add(runStart);
                    }
                }
                else
                {
                    i++;
                }
            }

            return runOffsets;
        }

        /// <summary>
        /// Attempts to read the .map data size (4 bytes big-endian) located
        /// immediately before the earliest '{' among all entities.
        /// Returns a tuple: (mapSize, offsetOfSize).
        /// If not found/invalid, returns null.
        /// </summary>
        public static (int mapSize, int offsetOfSize)? GetMapDataSizeAndOffset(ZoneFile zone, List<MapEntity> entities)
        {
            if (zone?.Data == null || entities == null || entities.Count == 0)
                return null;

            byte[] data = zone.Data;

            // 1) Find the earliest offset where a '{' was found
            int minOffset = int.MaxValue;
            foreach (var ent in entities)
            {
                if (ent.SourceOffset < minOffset)
                    minOffset = ent.SourceOffset;
            }

            // The size field is presumably 4 bytes before that offset
            int sizeOffset = minOffset - 4;
            if (sizeOffset < 0 || sizeOffset + 3 >= data.Length)
                return null; // out of range

            // 2) Read 4 bytes big-endian => mapSize
            int mapSize = (data[sizeOffset] << 24)
                        | (data[sizeOffset + 1] << 16)
                        | (data[sizeOffset + 2] << 8)
                        | (data[sizeOffset + 3]);

            // Basic validity check
            if (mapSize <= 0 || mapSize > data.Length)
                return null;

            return (mapSize, sizeOffset);
        }
    }
}
