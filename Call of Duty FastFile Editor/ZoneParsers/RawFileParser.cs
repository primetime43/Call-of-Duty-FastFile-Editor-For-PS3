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
        public static RawFileNode ExtractRawFileNodeNoPattern(FastFile openedFastFile, int offset)
        {
            Debug.WriteLine($"[ExtractRawFileNodeNoPattern] Starting raw file scan at offset 0x{offset:X}.");

            RawFileNode node = new RawFileNode();
            byte[] fileData = openedFastFile.OpenedFastFileZone.ZoneFileData;
            Debug.WriteLine($"[ExtractRawFileNodeNoPattern] Read file '{openedFastFile.ZoneFilePath}' ({fileData.Length} bytes).");


            for (int idx = 0; idx < openedFastFile.OpenedFastFileZone.ZoneFileAssets.ZoneAssetsPool.Count; idx++)
            {
                // Ensure we have at least 12 bytes for the header.
                if (offset > fileData.Length - 12)
                {
                    Debug.WriteLine($"[RawFile {idx}] Not enough bytes remaining for a header at offset 0x{offset:X}.");
                    break;
                }

                // --- Read the 12-byte header ---
                // Bytes 0-3: Marker. Must be 0xFFFFFFFF.
                uint marker = Utilities.ReadUInt32BigEndian(fileData, offset);
                if (marker != 0xFFFFFFFF)
                {
                    Debug.WriteLine($"[RawFile {idx}] Unexpected marker at offset 0x{offset:X}: 0x{marker:X}. Stopping extraction.");
                    break;
                }
                // Bytes 4-7: Data length. (This field holds the size of the file data.)
                int dataLength = (int)Utilities.ReadUInt32BigEndian(fileData, offset + 4);
                // Bytes 8-11: Name pointer. If -1, the file name is inline.
                int namePointer = (int)Utilities.ReadUInt32BigEndian(fileData, offset + 8);
                Debug.WriteLine($"[RawFile {idx}] Header at offset 0x{offset:X}: dataLength = {dataLength}, namePointer = {namePointer}");

                // Record where this header started.
                node.StartOfFileHeader = offset;
                node.MaxSize = dataLength; // Use dataLength as the file's size.

                // Advance offset past the 12-byte header.
                offset += 12;

                // --- Process the file name ---
                if (namePointer == -1)
                {
                    // Inline file name: read a null-terminated UTF8 string.
                    string inlineName = Utilities.ReadNullTerminatedString(fileData, offset);
                    node.FileName = inlineName;
                    Debug.WriteLine($"[RawFile {idx}] Inline name read: '{inlineName}'.");
                    // Determine the number of bytes read (including the null terminator).
                    int nameByteCount = Encoding.UTF8.GetByteCount(inlineName) + 1;
                    offset += nameByteCount;
                }
                else
                {
                    Debug.WriteLine($"[RawFile {idx}] Name pointer is {namePointer} (external).");
                    node.FileName = "[External]";
                }

                // --- Process the file data ---
                if (dataLength >= 0 && offset + dataLength <= fileData.Length)
                {
                    byte[] rawBytes = new byte[dataLength];
                    Array.Copy(fileData, offset, rawBytes, 0, dataLength);
                    node.RawFileBytes = rawBytes;
                    node.RawFileContent = Encoding.UTF8.GetString(rawBytes);
                    Debug.WriteLine($"[RawFile {idx}] Inline file data read: {rawBytes.Length} bytes.");
                }
                else
                {
                    Debug.WriteLine($"[RawFile {idx}] Data length is {dataLength} or exceeds file length; skipping file data read.");
                    node.RawFileBytes = new byte[0];
                    node.RawFileContent = string.Empty;
                }
                offset += dataLength; // Advance offset past the file data.
            }

            return node;
        }
        #endregion

        #region Pattern Parsing

        /// <summary>
        /// startOffset is where to start the search for the pattern
        /// Helps us get back on track to skip items I don't yet know how to parse
        /// </summary>
        /// <param name="startOffset"></param>
        /// <returns></returns>
        public static void GetRawFilesWithPattern(string zoneFilePath, out List<RawFileNode> rawFiles, int startOffset)
        {
            List<RawFileNode> rawFileNodes = ExtractRawFilesSizeAndName(zoneFilePath);
            rawFiles = rawFileNodes.Select(rawFileNode => new RawFileNode(rawFileNode.FileName, rawFileNode.RawFileBytes)).ToList();
        }

        /// <summary>
        /// Extracts the raw file entries from the FastFile (.ff) zone with their size and name.
        /// Setting raw file objects to the list of extracted file entries.
        /// THIS IS USING PATTERN MATCHING (don't use eventually)
        /// </summary>
        /// <param name="zoneFilePath"></param>
        /// <returns></returns>
        public static List<RawFileNode> ExtractRawFilesSizeAndName(string zoneFilePath)
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
