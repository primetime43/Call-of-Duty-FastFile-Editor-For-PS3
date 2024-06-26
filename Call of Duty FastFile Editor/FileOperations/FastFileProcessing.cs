﻿using Ionic.Zlib;
using System.Net;
using System.Text;

namespace Call_of_Duty_FastFile_Editor.IO
{
    public class FileEntryNode
    {
        public TreeNode Node { get; set; }

        /// <summary>
        /// This is where one of the patterns was found in the file from <see cref="FastFileProcessing.ExtractFileEntriesWithSizeAndName(string)"/>
        /// </summary>
        public int PatternIndexPosition { get; set; }
        public int MaxSize { get; set; }
        /// <summary>
        /// Gets the position where the file header starts. 
        /// This is also where the size of the file is located.
        /// </summary>
        public int StartOfFileHeader { get; set; }

        private int _codeStartPosition;

        /// <summary>
        /// Gets the position where the code starts, calculated as StartOfFileHeader + 8 + Node.Text.Length + 1.
        /// </summary>
        public int CodeStartPosition
        {
            get => StartOfFileHeader + 8 + Node.Text.Length + 1;
            set => _codeStartPosition = value; // Allow setting it manually if needed
        }

        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        public string FileName { get; set; }

        public int CodeEndPosition => CodeStartPosition + MaxSize;
    }

    public static class FastFileProcessing
    {
        public static void DecompressFastFile(string inputFilePath, string outputFilePath)
        {
            using (BinaryReader binaryReader = new BinaryReader(new FileStream(inputFilePath, FileMode.Open), Encoding.Default))
            using (BinaryWriter binaryWriter = new BinaryWriter(new FileStream(outputFilePath, FileMode.Create), Encoding.Default))
            {
                binaryReader.BaseStream.Position = 12L;

                try
                {
                    for (int i = 1; i < 5000; i++)
                    {
                        byte[] array = binaryReader.ReadBytes(2);
                        string text = BitConverter.ToString(array).Replace("-", "");
                        int count = int.Parse(text, System.Globalization.NumberStyles.AllowHexSpecifier);
                        byte[] compressedData = binaryReader.ReadBytes(count);
                        byte[] decompressedData = FastFileDecompressor.DecompressFF(compressedData);
                        binaryWriter.Write(decompressedData);
                    }
                }
                catch (Exception ex)
                {
                    if (!(ex is FormatException))
                    {
                        throw;
                    }
                }
            }
        }

        public static void RecompressFastFile(string ffFilePath, string zoneFilePath, FastFileHeader headerInfo)
        {
            byte[] header = new byte[8] { 73, 87, 102, 102, 117, 49, 48, 48 }; // IWffu100 header

            using (BinaryReader binaryReader = new BinaryReader(new FileStream(zoneFilePath, FileMode.Open), Encoding.Default))
            using (BinaryWriter binaryWriter = new BinaryWriter(new FileStream(ffFilePath, FileMode.Create), Encoding.Default))
            {
                // Write header to the new file
                binaryWriter.Write(header);

                if (headerInfo.IsCod5File)
                {
                    binaryWriter.Write(new byte[4] { 0, 0, 1, 131 });
                }
                else if (headerInfo.IsCod4File)
                {
                    binaryWriter.Write(new byte[4] { 0, 0, 0, 1 });
                }

                // Set the reader position to skip the header already written
                //binaryReader.BaseStream.PatternIndexPosition = 0; // Start from the beginning of the zone file
                binaryReader.BaseStream.Position = 12L; // Skip the original header

                int chunkSize = 65536;
                while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
                {
                    // Read a chunk
                    byte[] chunk = binaryReader.ReadBytes(chunkSize);

                    // Compress the chunk
                    byte[] compressedChunk = FastFileCompressor.CompressFF(chunk);

                    // Write the length of the compressed chunk (2 bytes)
                    int compressedLength = compressedChunk.Length;
                    byte[] lengthBytes = BitConverter.GetBytes(compressedLength);
                    Array.Reverse(lengthBytes); // Make sure the length is in correct byte order
                    binaryWriter.Write(lengthBytes, 2, 2); // Write only the last 2 bytes

                    // Write the compressed chunk
                    binaryWriter.Write(compressedChunk);
                }

                // Write the final 2-byte sequence
                binaryWriter.Write(new byte[2] { 0, 1 });
            }
        }

        public static List<FileEntryNode> ExtractFileEntriesWithSizeAndName(string filePath)
        {
            List<FileEntryNode> fileEntryNodes = new List<FileEntryNode>();
            byte[] fileData = File.ReadAllBytes(filePath);

            // Define the byte patterns to search for
            byte[][] patterns = new byte[][]
            {
                new byte[] { 0x2E, 0x63, 0x66, 0x67, 0x00 }, // .cfg
                new byte[] { 0x2E, 0x67, 0x73, 0x63, 0x00 }, // .gsc
                new byte[] { 0x2E, 0x61, 0x74, 0x72, 0x00 }, // .atr
                new byte[] { 0x2E, 0x63, 0x73, 0x63, 0x00 }, // .csc
                new byte[] { 0x2E, 0x72, 0x6D, 0x62, 0x00 }, // .rmb
                new byte[] { 0x2E, 0x61, 0x72, 0x65, 0x6E, 0x61, 0x00 }, // .arena
                new byte[] { 0x2E, 0x76, 0x69, 0x73, 0x69, 0x6F, 0x6E, 0x00 } // .vision
            };

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
                        byte[] matchedBytes = new byte[pattern.Length];
                        Array.Copy(fileData, patternIndex, matchedBytes, 0, pattern.Length);
                        string matchedBytesHex = BitConverter.ToString(matchedBytes).Replace("-", " ");
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
                            //MessageBox.Show($"Skipping invalid FF FF FF FF sequence at {ffffPosition:X}", "Invalid Sequence Debug Info");
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
                                TreeNode treeNode = new TreeNode(fileName)
                                {
                                    Tag = patternIndex
                                };
                                fileEntryNodes.Add(new FileEntryNode
                                {
                                    Node = treeNode,
                                    PatternIndexPosition = patternIndex,
                                    MaxSize = maxSize,
                                    StartOfFileHeader = sizePosition,
                                    FileName = fileName
                                });

                                // Debugging message box
                                //MessageBox.Show($"Pattern: {BitConverter.ToString(pattern).Replace("-", "\\x")}\nPattern Index: {patternIndex:X}\nFile Name: {fileName}\nSize PatternIndexPosition: {sizePosition:X}\nMax Size: {maxSize}\nHeader Start: {sizePosition:X}", "ExtractFileEntriesWithSizeAndName Debug Info");
                            }
                            else
                            {
                                //MessageBox.Show($"Pattern: {BitConverter.ToString(pattern).Replace("-", "\\x")}\nPattern Index: {patternIndex:X}\nFile Name Start: {fileNameStart:X}\nFile Name Invalid or Contains Null", "ExtractFileEntriesWithSizeAndName Debug Info");
                            }
                        }
                        else
                        {
                            //MessageBox.Show($"Pattern: {BitConverter.ToString(pattern).Replace("-", "\\x")}\nPattern Index: {patternIndex:X}\nFFFF PatternIndexPosition: {ffffPosition:X}\nSize PatternIndexPosition Invalid: {sizePosition:X}", "ExtractFileEntriesWithSizeAndName Debug Info");
                        }
                    }
                }
            }

            fileEntryNodes.Sort((a, b) => a.PatternIndexPosition.CompareTo(b.PatternIndexPosition));
            return fileEntryNodes;
        }

        public static string ExtractFullFileName(byte[] data, int fileNameStart)
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

            // Debugging message box
            //MessageBox.Show($"Extracted File Name: {fileName}\nFile Name Start: {fileNameStart:X}", "ExtractFullFileName Debug Info");

            return fileName.ToString();
        }

        public static string ReadFileContentAfterName(string filePath, int startPosition, int maxSize)
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

            return RemoveZeroPadding(contentBytes);
        }

        private static string RemoveZeroPadding(byte[] content)
        {
            // Remove zero padding (0x00) from the content
            int i = content.Length - 1;
            while (i >= 0 && content[i] == 0x00)
            {
                i--;
            }

            // Convert the remaining content to a string
            return Encoding.Default.GetString(content, 0, i + 1);
        }

    }
}