using Call_of_Duty_FastFile_Editor.Models;
using Call_of_Duty_FastFile_Editor.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Call_of_Duty_FastFile_Editor.ZoneParsers
{
    public class LocalizeAssetParser
    {
        /// <summary>
        /// Parses a single localized asset starting at the given offset in the zone file data.
        /// Expected pattern:
        ///   [8 bytes marker: 0xFF 0xFF 0xFF 0xFF 0xFF 0xFF 0xFF 0xFF]
        ///   [LocalizedText: null-terminated ASCII string]
        ///   [Key: null-terminated ASCII string]
        /// Returns a tuple containing the parsed LocalizedEntry (or null if not found)
        /// and the new offset immediately after the entry.
        /// </summary>
        /// <param name="openedFastFile">The FastFile object holding zone data.</param>
        /// <param name="startingOffset">Offset in the zone data where the localized item is expected to start.</param>
        /// <returns>
        /// A tuple of (LocalizedEntry entry, int nextOffset). If no valid entry is found, entry is null.
        /// </returns>
        public static (LocalizedEntry entry, int nextOffset) ParseSingleLocalizeAssetNoPattern(FastFile openedFastFile, int startingOffset)
        {
            Debug.WriteLine($"[LocalizeAssetParser] Starting parse at offset 0x{startingOffset:X}.");
            byte[] fileData = openedFastFile.OpenedFastFileZone.Data;

            using (MemoryStream ms = new MemoryStream(fileData))
            using (BinaryReader br = new BinaryReader(ms, Encoding.ASCII))
            {
                ms.Position = startingOffset;

                // Ensure there are at least 8 bytes available for the marker.
                if (ms.Position + 8 > ms.Length)
                {
                    Debug.WriteLine("[LocalizeAssetParser] Not enough bytes for marker. Returning null.");
                    return (null, startingOffset);
                }

                int markerPos = (int)ms.Position;
                byte[] markerBytes = br.ReadBytes(8);
                foreach (byte b in markerBytes)
                {
                    if (b != 0xFF)
                    {
                        Debug.WriteLine($"[LocalizeAssetParser] Expected eight 0xFF bytes at position 0x{markerPos:X} but marker is invalid. Returning null.");
                        return (null, markerPos);
                    }
                }

                // After the marker, the localized text begins.
                int entryStart = (int)ms.Position;

                // Read the localized text (null-terminated).
                string localizedText = ReadNullTerminatedString(br);
                if (localizedText == null)
                {
                    Debug.WriteLine("[LocalizeAssetParser] Localized text string not found. Returning null.");
                    return (null, (int)ms.Position);
                }

                // Read the key (null-terminated).
                string key = ReadNullTerminatedString(br);
                if (key == null)
                {
                    Debug.WriteLine("[LocalizeAssetParser] Key string not found. Returning null.");
                    return (null, (int)ms.Position);
                }

                int entryEnd = (int)ms.Position;

                LocalizedEntry entry = new LocalizedEntry
                {
                    Key = key,
                    LocalizedText = localizedText,
                    StartOfFileHeader = entryStart,
                    EndOfFileHeader = entryEnd
                };

                int nextOffset = (int)ms.Position;
                Debug.WriteLine($"[LocalizeAssetParser] Parsed entry with key: {entry.Key}, localized text length: {entry.LocalizedText.Length}, entry range: 0x{entryStart:X}-0x{entryEnd:X}.");
                return (entry, nextOffset);
            }
        }

        /// <summary>
        /// Searches for the eight 0xFF bytes pattern starting from the given offset,
        /// then parses a single localized asset entry if found.
        /// Expected pattern:
        ///   [8 bytes marker: 0xFF 0xFF 0xFF 0xFF 0xFF 0xFF 0xFF 0xFF]
        ///   [LocalizedText: null-terminated ASCII string]
        ///   [Key: null-terminated ASCII string]
        /// Returns a tuple of (LocalizedEntry, nextOffset). If the pattern is not found or not followed by valid data, returns (null, startingOffset).
        /// </summary>
        public static (LocalizedEntry entry, int nextOffset) ParseSingleLocalizeAssetWithPattern(FastFile openedFastFile, int startingOffset)
        {
            Debug.WriteLine($"[LocalizeAssetParser] Starting pattern-based parse at offset 0x{startingOffset:X}.");
            byte[] fileData = openedFastFile.OpenedFastFileZone.Data;
            int markerPos = -1;

            // Search for eight consecutive 0xFF bytes starting at startingOffset.
            for (int pos = startingOffset; pos <= fileData.Length - 8; pos++)
            {
                bool isMarker = true;
                for (int j = 0; j < 8; j++)
                {
                    if (fileData[pos + j] != 0xFF)
                    {
                        isMarker = false;
                        break;
                    }
                }
                if (isMarker)
                {
                    // Ensure that after the eight FFs, there is actual data (i.e. not more FFs or a null terminator)
                    if (pos + 8 < fileData.Length)
                    {
                        byte candidateByte = fileData[pos + 8];
                        if (candidateByte == 0xFF || candidateByte == 0x00)
                        {
                            // Not valid data; continue searching for a valid marker.
                            continue;
                        }
                    }
                    markerPos = pos;
                    break;
                }
            }
            if (markerPos < 0)
            {
                Debug.WriteLine("[LocalizeAssetParser] Marker pattern not found or not followed by valid data. Returning null.");
                return (null, startingOffset);
            }

            using (MemoryStream ms = new MemoryStream(fileData))
            using (BinaryReader br = new BinaryReader(ms, Encoding.ASCII))
            {
                ms.Position = markerPos + 8; // Skip the 8-byte marker.
                int entryStart = (int)ms.Position;

                // Read the localized text (null-terminated).
                string localizedText = ReadNullTerminatedString(br);
                if (localizedText == null)
                {
                    Debug.WriteLine("[LocalizeAssetParser] Localized text string not found. Returning null.");
                    return (null, (int)ms.Position);
                }

                // Read the key (null-terminated).
                string key = ReadNullTerminatedString(br);
                if (key == null)
                {
                    Debug.WriteLine("[LocalizeAssetParser] Key string not found. Returning null.");
                    return (null, (int)ms.Position);
                }

                int entryEnd = (int)ms.Position;
                LocalizedEntry entry = new LocalizedEntry
                {
                    Key = key,
                    LocalizedText = localizedText,
                    StartOfFileHeader = entryStart,
                    EndOfFileHeader = entryEnd
                };

                int nextOffset = (int)ms.Position;
                Debug.WriteLine($"[LocalizeAssetParser] Parsed entry with key: {entry.Key}, entry range: 0x{entryStart:X}-0x{entryEnd:X}.");
                return (entry, nextOffset);
            }
        }

        /// <summary>
        /// Reads a null-terminated ASCII string from the current position of the BinaryReader.
        /// Returns null if no bytes are available.
        /// </summary>
        /// <param name="br">The BinaryReader instance.</param>
        /// <returns>The read string, or null if unable to read.</returns>
        private static string ReadNullTerminatedString(BinaryReader br)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                while (true)
                {
                    if (br.BaseStream.Position >= br.BaseStream.Length)
                        return null;
                    byte b = br.ReadByte();
                    if (b == 0x00)
                        break;
                    sb.Append((char)b);
                }
            }
            catch (EndOfStreamException)
            {
                return null;
            }
            return sb.ToString();
        }
    }
}
