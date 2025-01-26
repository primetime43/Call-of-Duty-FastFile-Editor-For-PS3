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

        /// <summary>
        /// This existing method tries to automatically locate the .map block
        /// near the end of the file, scanning backward for a plausible big-endian length
        /// followed by ASCII data that includes '{'.
        /// 
        /// MAYBE DELETE THIS
        /// </summary>
        public static List<MapEntity> ParseMapEnts(Zone zone)
        {
            var results = new List<MapEntity>();
            byte[] zoneBytes = zone.FileData;
            if (zoneBytes == null || zoneBytes.Length < 8)
                return results;

            // We'll scan backwards, starting from near the end.
            int startIndex = zoneBytes.Length - 4;

            for (int i = startIndex; i >= 0; i--)
            {
                if (i + 3 >= zoneBytes.Length)
                    continue;

                int length = (zoneBytes[i] << 24)
                           | (zoneBytes[i + 1] << 16)
                           | (zoneBytes[i + 2] << 8)
                           | (zoneBytes[i + 3]);

                if (length <= 0 || length > zoneBytes.Length)
                    continue;

                int startOfMapData = i + 4;
                int endOfMapData = startOfMapData + length;
                if (endOfMapData > zoneBytes.Length)
                    continue;

                byte[] mapDataBytes = new byte[length];
                Buffer.BlockCopy(zoneBytes, startOfMapData, mapDataBytes, 0, length);

                // Quick check for brace
                if (Array.IndexOf(mapDataBytes, (byte)'{') < 0)
                    continue;

                string mapText = Encoding.ASCII.GetString(mapDataBytes);

                var parsed = ParseMapString(mapText);
                if (parsed.Count > 0)
                {
                    results.AddRange(parsed);
                    // If you only expect one block, break here
                    break;
                }
            }

            return results;
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

            // Split text into lines, ignoring empty lines
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
                    if (currentEntity != null)
                        entities.Add(currentEntity);
                    currentEntity = null;
                    insideBraces = false;
                    continue;
                }

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
