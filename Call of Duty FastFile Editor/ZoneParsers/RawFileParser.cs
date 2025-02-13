using Call_of_Duty_FastFile_Editor.Models;
using Call_of_Duty_FastFile_Editor.Services;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace Call_of_Duty_FastFile_Editor.ZoneParsers
{
    public static class RawFileParser
    {
        #region Structure Parsing
        public static RawFileNode ExtractSingleRawFileNodeNoPattern(FastFile openedFastFile, int offset)
        {
            Debug.WriteLine($"[ExtractSingleRawFileNodeNoPattern] Starting raw file scan at offset 0x{offset:X}.");

            RawFileNode node = new RawFileNode();
            byte[] fileData = openedFastFile.OpenedFastFileZone.ZoneFileData;
            Debug.WriteLine($"[ExtractSingleRawFileNodeNoPattern] Read file '{openedFastFile.ZoneFilePath}' ({fileData.Length} bytes).");

            // Ensure we have enough bytes for the header (12 bytes)
            if (offset > fileData.Length - 12)
            {
                Debug.WriteLine($"[RawFile] Not enough bytes remaining for a header at offset 0x{offset:X}.");
                return null;
            }

            // Read and validate the first marker (should be 0xFFFFFFFF)
            uint marker1 = Utilities.ReadUInt32BigEndian(fileData, offset);
            if (marker1 != 0xFFFFFFFF)
            {
                Debug.WriteLine($"[RawFile] Unexpected marker at offset 0x{offset:X}: 0x{marker1:X}.");
                return null;
            }

            // Read the data length (size of the file data)
            int dataLength = (int)Utilities.ReadUInt32BigEndian(fileData, offset + 4);
            if (dataLength == 0)
            {
                Debug.WriteLine($"[RawFile] dataLength is 0 at offset 0x{offset + 4:X}. Probably not a rawfile. Returning null.");
                return null;
            }

            // Read and validate the second marker (should be 0xFFFFFFFF)
            uint marker2 = Utilities.ReadUInt32BigEndian(fileData, offset + 8);
            if (marker2 != 0xFFFFFFFF)
            {
                Debug.WriteLine($"[RawFile] Unexpected second marker at offset 0x{offset + 8:X}: 0x{marker2:X}.");
                return null;
            }

            // Record the start of the header and file size
            node.StartOfFileHeader = offset;
            node.MaxSize = dataLength;

            // Move past the 12-byte header to read the file name
            int fileNameOffset = offset + 12;
            string inlineName = Utilities.ReadNullTerminatedString(fileData, fileNameOffset);
            node.FileName = inlineName;
            Debug.WriteLine($"[RawFile] Inline name read: '{inlineName}'.");

            // Calculate the total bytes consumed by the file name (including null terminator)
            int nameByteCount = Encoding.UTF8.GetByteCount(inlineName) + 1;

            // File data starts immediately after the file name
            int fileDataOffset = fileNameOffset + nameByteCount;

            // Ensure there's enough data for the file data
            if (fileDataOffset + dataLength <= fileData.Length)
            {
                byte[] rawBytes = new byte[dataLength];
                Array.Copy(fileData, fileDataOffset, rawBytes, 0, dataLength);
                node.RawFileBytes = rawBytes;
                node.RawFileContent = Encoding.UTF8.GetString(rawBytes);
                Debug.WriteLine($"[RawFile] Inline file data read: {rawBytes.Length} bytes.");
            }
            else
            {
                Debug.WriteLine($"[RawFile] Data length {dataLength} exceeds available file data; skipping file data read.");
                node.RawFileBytes = new byte[0];
                node.RawFileContent = string.Empty;
            }

            return node;
        }
        #endregion

        #region Pattern Parsing

        /// <summary>
        /// Scans the zone file (at zoneFilePath) starting at startOffset for the first raw file entry
        /// that matches one of the defined patterns. When found, extracts its size, file name, content,
        /// and returns a RawFileNode. If no match is found, returns null.
        /// </summary>
        /// <param name="zoneFilePath">Path to the zone file.</param>
        /// <param name="startOffset">
        /// The offset in the zone file at which to start scanning for the raw file header.
        /// (Pass 0 to search from the beginning.)
        /// </param>
        /// <returns>The first matching RawFileNode or null if none is found.</returns>
        public static RawFileNode ExtractSingleRawFileNodeWithPattern(string zoneFilePath, int startOffset = 0)
        {
            byte[] fileData = File.ReadAllBytes(zoneFilePath);
            Debug.WriteLine($"[ExtractSingleRawFileNodeWithPattern] Read file '{zoneFilePath}' ({fileData.Length} bytes).");
            byte[][] patterns = Constants.RawFiles.FileNamePatterns;

            using (BinaryReader binaryReader = new BinaryReader(new MemoryStream(fileData), Encoding.Default))
            {
                foreach (var pattern in patterns)
                {
                    for (int i = startOffset; i <= fileData.Length - pattern.Length; i++)
                    {
                        bool match = true;
                        for (int j = 0; j < pattern.Length; j++)
                        {
                            if (fileData[i + j] != pattern[j])
                            {
                                match = false;
                                break;
                            }
                        }
                        if (!match)
                            continue;

                        int patternIndex = i;
                        Debug.WriteLine($"[ExtractSingleRawFileNodeWithPattern] Pattern found at offset 0x{patternIndex:X}.");

                        // Example struture working backwards
                        // FF FF FF FF NamePointer (start of header)
                        // 00 00 00 00 Size - sizePosition
                        // 00 00 00 00 DataPointer - this is what ffffPositionBeforeName represents
                        // Name (null-terminated) - so we're working backwards from here

                        // Move backwards to locate a sequence of 4 FF bytes before the name.
                        int ffffPositionBeforeName = patternIndex - 1;
                        while (ffffPositionBeforeName >= 4 &&
                               !(fileData[ffffPositionBeforeName] == 0xFF &&
                                 fileData[ffffPositionBeforeName - 1] == 0xFF &&
                                 fileData[ffffPositionBeforeName - 2] == 0xFF &&
                                 fileData[ffffPositionBeforeName - 3] == 0xFF))
                        {
                            ffffPositionBeforeName--;
                        }

                        if (ffffPositionBeforeName < 4 || fileData[ffffPositionBeforeName + 1] == 0x00)
                        {
                            Debug.WriteLine($"[ExtractSingleRawFileNodeWithPattern] Invalid FF sequence at offset 0x{ffffPositionBeforeName:X}. Continuing.");
                            continue;
                        }

                        // The file size (maxSize) is stored just before the 4-FF sequence.
                        int sizePosition = ffffPositionBeforeName - 7;
                        if (sizePosition < 0)
                        {
                            Debug.WriteLine($"[ExtractSingleRawFileNodeWithPattern] Size position invalid (sizePosition = {sizePosition}). Continuing.");
                            continue;
                        }

                        int startOfHeaderPosition = sizePosition - 4; // FF FF FF FF before the size position raw file name pointer

                        binaryReader.BaseStream.Position = sizePosition;
                        int maxSize = IPAddress.NetworkToHostOrder(binaryReader.ReadInt32());
                        Debug.WriteLine($"[ExtractSingleRawFileNodeWithPattern] Found size = {maxSize} at offset 0x{sizePosition:X}.");

                        // The file name is assumed to start immediately after the 4-FF sequence.
                        int fileNameStart = ffffPositionBeforeName + 1;
                        string fileName = ExtractFullFileName(fileData, fileNameStart);
                        Debug.WriteLine($"[ExtractSingleRawFileNodeWithPattern] Extracted name: '{fileName}' at offset 0x{fileNameStart:X}.");

                        if (!string.IsNullOrEmpty(fileName) && !fileName.Contains("\x00"))
                        {
                            string fileContent = ReadFileContentAfterName(zoneFilePath, patternIndex, maxSize);
                            byte[] fileBytes = ExtractBinaryContent(zoneFilePath, patternIndex, maxSize);

                            return new RawFileNode
                            {
                                PatternIndexPosition = patternIndex,
                                MaxSize = maxSize,
                                StartOfFileHeader = startOfHeaderPosition,
                                FileName = fileName,
                                RawFileContent = fileContent,
                                RawFileBytes = fileBytes
                            };
                        }
                    }
                }
            }
            Debug.WriteLine("[ExtractSingleRawFileNodeWithPattern] No matching raw file node found.");
            return null;
        }

        /// <summary>
        /// Extracts the raw file entries from the FastFile (.ff) zone with their size and name.
        /// Setting raw file objects to the list of extracted file entries.
        /// THIS IS USING PATTERN MATCHING (don't use eventually)
        /// </summary>
        /// <param name="zoneFilePath"></param>
        /// <returns></returns>
        public static List<RawFileNode> ExtractAllRawFilesSizeAndName(string zoneFilePath)
        {
            List<RawFileNode> rawFileNodes = new List<RawFileNode>();
            byte[] fileData = File.ReadAllBytes(zoneFilePath);

            // Use the centralized patterns from Constants
            byte[][] patterns = Constants.RawFiles.FileNamePatterns;

            using (BinaryReader binaryReader = new BinaryReader(new MemoryStream(fileData), Encoding.Default))
            {
                foreach (var pattern in patterns)
                {
                    for (int i = 0; i <= fileData.Length - pattern.Length; i++)
                    {
                        // Check if the pattern matches exactly at this position
                        bool match = true;
                        for (int j = 0; j < pattern.Length; j++)
                        {
                            if (fileData[i + j] != pattern[j])
                            {
                                match = false;
                                break;
                            }
                        }

                        if (!match)
                        {
                            continue;
                        }

                        int patternIndex = i;

                        // Debugging information: show the matched bytes
                        //byte[] matchedBytes = new byte[pattern.Length];
                        //Array.Copy(fileData, patternIndex, matchedBytes, 0, pattern.Length);
                        //string matchedBytesHex = BitConverter.ToString(matchedBytes).Replace("-", " ");
                        //MessageBox.Show($"Pattern: {BitConverter.ToString(pattern).Replace("-", "\\x")}\nPattern Index: {patternIndex:X}\nMatched Bytes: {matchedBytesHex}", "Pattern Match Debug Info");

                        // Move backwards to find the FF FF FF FF sequence
                        int ffffPosition = patternIndex - 1;
                        while (ffffPosition >= 4 && !(fileData[ffffPosition] == 0xFF && fileData[ffffPosition - 1] == 0xFF && fileData[ffffPosition - 2] == 0xFF && fileData[ffffPosition - 3] == 0xFF))
                        {
                            ffffPosition--;
                        }

                        // Ensure the sequence is valid (not followed by \x00 and not part of a different structure)
                        if (ffffPosition < 4 || fileData[ffffPosition + 1] == 0x00)
                        {
                            continue;
                        }

                        // The size is stored right before the FF FF FF FF sequence
                        int sizePosition = ffffPosition - 7;
                        if (sizePosition >= 0)
                        {
                            binaryReader.BaseStream.Position = sizePosition;
                            int maxSize = IPAddress.NetworkToHostOrder(binaryReader.ReadInt32());

                            // The file name starts right after the FF FF FF FF sequence
                            int fileNameStart = ffffPosition + 1;
                            string fileName = ExtractFullFileName(fileData, fileNameStart);

                            if (!string.IsNullOrEmpty(fileName) && !fileName.Contains("\x00"))
                            {
                                // Read the file content
                                string fileContent = ReadFileContentAfterName(zoneFilePath, patternIndex, maxSize);

                                byte[] fileBytes = null;
                                // Extract binary data
                                fileBytes = ExtractBinaryContent(zoneFilePath, patternIndex, maxSize);

                                rawFileNodes.Add(new RawFileNode
                                {
                                    PatternIndexPosition = patternIndex,
                                    MaxSize = maxSize,
                                    StartOfFileHeader = sizePosition,
                                    FileName = fileName,
                                    RawFileContent = fileContent,
                                    RawFileBytes = fileBytes
                                });
                            }
                        }
                    }
                }
            }

            rawFileNodes.Sort((a, b) => a.PatternIndexPosition.CompareTo(b.PatternIndexPosition));
            return rawFileNodes;
        }
        #endregion

        #region Helper Methods
        private static byte[] ExtractBinaryContent(string filePath, int patternIndex, int maxSize)
        {
            byte[] fileData = File.ReadAllBytes(filePath);

            // Move to the position after the name's null terminator
            int contentStartPosition = patternIndex;
            while (contentStartPosition < fileData.Length && fileData[contentStartPosition] != 0x00)
            {
                contentStartPosition++;
            }
            // Skip the null terminator
            contentStartPosition++;

            // Calculate content length up to max size
            int contentLength = maxSize;
            if (contentStartPosition + contentLength > fileData.Length)
            {
                contentLength = fileData.Length - contentStartPosition;
            }

            byte[] contentBytes = new byte[contentLength];
            Array.Copy(fileData, contentStartPosition, contentBytes, 0, contentLength);

            return RemoveZeroPadding(contentBytes);
        }

        private static string ExtractFullFileName(byte[] data, int fileNameStart)
        {
            StringBuilder fileName = new StringBuilder();

            // Read the file name from fileNameStart until the null terminator
            for (int i = fileNameStart; i < data.Length; i++)
            {
                char c = (char)data[i];
                if (c == '\0')
                {
                    break;
                }
                fileName.Append(c);
            }

            return fileName.ToString();
        }

        private static string ReadFileContentAfterName(string filePath, int startPosition, int maxSize)
        {
            byte[] fileData = File.ReadAllBytes(filePath);

            // Move to the position after the name's null terminator
            int contentStartPosition = startPosition;
            while (contentStartPosition < fileData.Length && fileData[contentStartPosition] != 0x00)
            {
                contentStartPosition++;
            }
            // Skip the null terminator
            contentStartPosition++;

            // Calculate content length up to max size
            int contentLength = maxSize;
            if (contentStartPosition + contentLength > fileData.Length)
            {
                contentLength = fileData.Length - contentStartPosition;
            }

            byte[] contentBytes = new byte[contentLength];
            Array.Copy(fileData, contentStartPosition, contentBytes, 0, contentLength);

            byte[] trimmedBytes = RemoveZeroPadding(contentBytes);
            return Encoding.Default.GetString(trimmedBytes);
        }

        private static byte[] RemoveZeroPadding(byte[] content)
        {
            // Remove zero padding (0x00) from the content
            int i = content.Length - 1;
            while (i >= 0 && content[i] == 0x00)
            {
                i--;
            }

            byte[] trimmedContent = new byte[i + 1];
            Array.Copy(content, 0, trimmedContent, 0, i + 1);
            return trimmedContent;
        }
        #endregion
    }
}
