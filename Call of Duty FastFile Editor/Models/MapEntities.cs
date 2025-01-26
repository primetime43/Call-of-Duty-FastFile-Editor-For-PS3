using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Call_of_Duty_FastFile_Editor.Models
{
    /// <summary>
    /// Represents a single .map-style block of key-value pairs.
    /// </summary>
    public class MapEntity
    {
        /// <summary>
        /// Stores key-value pairs (e.g. "classname" -> "worldspawn").
        /// </summary>
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }

    public static class MapEntityOperations
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
        /// <param name="zone">The zone whose FileData we are scanning.</param>
        /// <param name="offset">Offset where the 4-byte big-endian size is located.</param>
        public static List<MapEntity> ParseMapEntsAtOffset(Zone zone, int offset)
        {
            var results = new List<MapEntity>();
            byte[] zoneBytes = zone.FileData;
            if (zoneBytes == null)
                return results;

            // Check that we can read at least 4 bytes at 'offset'
            if (offset < 0 || offset + 4 > zoneBytes.Length)
                return results;

            // 1) Read 4 bytes as big-endian
            int length = (zoneBytes[offset + 0] << 24)
                       | (zoneBytes[offset + 1] << 16)
                       | (zoneBytes[offset + 2] << 8)
                       | (zoneBytes[offset + 3]);

            // Basic validity checks
            if (length <= 0 || length > zoneBytes.Length)
                return results;

            int startOfMapData = offset + 4;
            int endOfMapData = startOfMapData + length;
            if (endOfMapData > zoneBytes.Length)
                return results; // would run off the end

            // 2) Extract the map data bytes
            byte[] mapDataBytes = new byte[length];
            Buffer.BlockCopy(zoneBytes, startOfMapData, mapDataBytes, 0, length);

            // 3) Convert to text (usually ASCII in CoD zone files)
            string mapText = Encoding.ASCII.GetString(mapDataBytes);

            // 4) Parse the map text (blocks { ... })
            results = ParseMapString(mapText);
            return results;
        }

        public static int FindMapHeaderOffsetViaFF(Zone zone)
        {
            if (zone?.FileData == null)
                return -1;

            byte[] data = zone.FileData;
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
        /// Parses .map text with blocks like:
        /// {
        ///   "key" "value"
        ///   ...
        /// }
        /// into a list of MapEntity objects.
        /// </summary>
        private static List<MapEntity> ParseMapString(string mapText)
        {
            var entities = new List<MapEntity>();

            var lines = mapText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            MapEntity currentEntity = null;
            bool insideBraces = false;

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();

                if (line == "{")
                {
                    currentEntity = new MapEntity();
                    insideBraces = true;
                    continue;
                }

                if (line == "}")
                {
                    // Only add if currentEntity has at least 1 property
                    if (currentEntity != null && currentEntity.Properties.Count > 0)
                    {
                        entities.Add(currentEntity);
                    }
                    currentEntity = null;
                    insideBraces = false;
                    continue;
                }

                // Attempt to parse key-value lines only inside braces
                if (insideBraces && currentEntity != null)
                {
                    var match = LinePattern.Match(line);
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
    }
}
