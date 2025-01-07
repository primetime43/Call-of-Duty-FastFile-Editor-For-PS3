using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Call_of_Duty_FastFile_Editor.Models;

namespace Call_of_Duty_FastFile_Editor.IO
{
    public static class FastFileProcessing
    {
        public static void DecompressFastFile(string inputFilePath, string outputFilePath)
        {
            using (BinaryReader binaryReader = new BinaryReader(new FileStream(inputFilePath, FileMode.Open, FileAccess.Read), Encoding.Default))
            using (BinaryWriter binaryWriter = new BinaryWriter(new FileStream(outputFilePath, FileMode.Create, FileAccess.Write), Encoding.Default))
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
            byte[] header = new byte[8] { 73, 87, 102, 102, 117, 49, 48, 48 }; // "Iwffu100" header

            using (BinaryReader binaryReader = new BinaryReader(new FileStream(zoneFilePath, FileMode.Open, FileAccess.Read), Encoding.Default))
            using (BinaryWriter binaryWriter = new BinaryWriter(new FileStream(ffFilePath, FileMode.Create, FileAccess.Write), Encoding.Default))
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
                    Array.Reverse(lengthBytes); // Ensure correct byte order
                    binaryWriter.Write(lengthBytes, 2, 2); // Write only the last 2 bytes

                    // Write the compressed chunk
                    binaryWriter.Write(compressedChunk);
                }

                // Write the final 2-byte sequence
                binaryWriter.Write(new byte[2] { 0, 1 });
            }
        }

        /// <summary>
        /// Updates the content of a specific file within the zone file.
        /// </summary>
        /// <param name="zoneFilePath">Path to the decompressed zone file.</param>
        /// <param name="node">The RawFileNode representing the raw file to update.</param>
        /// <param name="newContent">New content as a byte array.</param>
        /// <exception cref="ArgumentException">Thrown when newContent exceeds MaxSize.</exception>
        /// <exception cref="IOException">Thrown when file operations fail.</exception>
        public static void UpdateFileContent(string zoneFilePath, RawFileNode rawFileNode, byte[] newContent)
        {
            if (newContent.Length > rawFileNode.MaxSize)
            {
                throw new ArgumentException($"New content size ({newContent.Length} bytes) exceeds the maximum allowed size ({rawFileNode.MaxSize} bytes) for file '{rawFileNode.FileName}'.");
            }

            try
            {
                using (FileStream fs = new FileStream(zoneFilePath, FileMode.Open, FileAccess.Write, FileShare.None))
                {
                    fs.Seek(rawFileNode.CodeStartPosition, SeekOrigin.Begin);
                    fs.Write(newContent, 0, newContent.Length);

                    // Pad with zeros if newContent is smaller than MaxSize
                    if (newContent.Length < rawFileNode.MaxSize)
                    {
                        byte[] padding = new byte[rawFileNode.MaxSize - newContent.Length];
                        fs.Write(padding, 0, padding.Length);
                    }
                }

                // Update the RawFileNode properties
                rawFileNode.RawFileBytes = newContent;
                rawFileNode.RawFileContent = Encoding.Default.GetString(newContent);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new IOException($"Failed to update content for raw file '{rawFileNode.FileName}': {ex.Message}", ex);
            }
        }

        public static List<RawFileNode> ExtractFileEntriesWithSizeAndName(string filePath)
        {
            List<RawFileNode> rawFileNodes = new List<RawFileNode>();
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
                                string fileContent = ReadFileContentAfterName(filePath, patternIndex, maxSize);

                                byte[] fileBytes = null;
                                // Extract binary data
                                fileBytes = ExtractBinaryContent(filePath, patternIndex, maxSize);

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

            if (i < 0)
            {
                // All bytes are zero
                return new byte[0];
            }

            byte[] trimmedContent = new byte[i + 1];
            Array.Copy(content, 0, trimmedContent, 0, i + 1);
            return trimmedContent;
        }
    }
}
