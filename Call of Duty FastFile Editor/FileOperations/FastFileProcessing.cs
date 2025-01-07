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
        /// This is used for injecting raw files into the zone.
        /// Does not append, only overwrites in-place.
        /// </summary>
        /// <param name="zoneFilePath">Path to the decompressed zone file.</param>
        /// <param name="node">The RawFileNode representing the raw file to update.</param>
        /// <param name="newContent">New content as a byte array.</param>
        /// <exception cref="IOException">Thrown when file operations fail.</exception>
        public static void UpdateFileContent(string zoneFilePath, RawFileNode rawFileNode, byte[] newContent)
        {
            // I think we want to get rid of this eventually.
            // Not sure yet, but the user may be able to create larger files if they just change the
            // max size in the header.
            
            
            
            
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

        // this would be for expanding the size of original raw file content & sizes (not custom raw files)
        // not yet implemented
        public static void ExpandAndUpdateFileContent(string zoneFilePath, RawFileNode existingRawFileNode, RawFileNode newRawFileNode, string newContent)
        {
            int oldSize = newRawFileNode.MaxSize;
            int newSize = newContent.Length;

            // newSize > oldSize, we must shift subsequent data
            int difference = newSize - oldSize;

            using (FileStream fs = new FileStream(zoneFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                // move all the data that starts after "rawFileNode.CodeStartPosition + oldSize"
                // to a position that is 'difference' bytes further.

                long dataToShiftStart = newRawFileNode.CodeStartPosition + oldSize;
                long dataToShiftEnd = fs.Length; // shift everything until EOF
                long dataToShiftLength = dataToShiftEnd - dataToShiftStart;

                // If there's data after the old file chunk:
                if (dataToShiftLength > 0)
                {
                    // We read that chunk into memory (or do a chunked approach)
                    fs.Seek(dataToShiftStart, SeekOrigin.Begin);
                    byte[] tailData = new byte[dataToShiftLength];
                    fs.Read(tailData, 0, tailData.Length);

                    // Move the stream pointer to where that data now belongs
                    fs.Seek(dataToShiftStart + difference, SeekOrigin.Begin);

                    // Write the tail data
                    fs.Write(tailData, 0, tailData.Length);
                }

                // Now the file is large enough to hold the bigger chunk
                // Write the new content
                fs.Seek(newRawFileNode.CodeStartPosition, SeekOrigin.Begin);
                //fs.Write(newContent.tob, 0, newContent.Length);

                // Update the 4-byte size in the zone file header
                //UpdateZoneHeaderSize(zoneFilePath, rawFileNode.StartOfFileHeader, newSize);
            }
        }

        /// <summary>
        /// Extracts the file entries from a Fast File (.ff) with their size and name.
        /// Setting raw file objects to the list of extracted file entries.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
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

            byte[] trimmedContent = new byte[i + 1];
            Array.Copy(content, 0, trimmedContent, 0, i + 1);
            return trimmedContent;
        }

        // Maybe move this elsewhere

        /// <summary>
        /// Constructs a byte array that contains:
        /// 
        ///   [ 4-byte big-endian size ]
        ///   [ 0xFF 0xFF 0xFF 0xFF ]
        ///   [ ASCII filename + 0x00 ]
        ///   [ raw content bytes (size) ]
        /// 
        /// This matches the format expected by ExtractFileEntriesWithSizeAndName.
        /// </summary>
        /// <param name="fileName">e.g. "myfile.gsc" or "myfile.cfg"</param>
        /// <param name="fileContent">Raw content of the file.</param>
        public static byte[] BuildNewRawFileEntry(string fileName, byte[] fileContent)
        {
            // 1) Convert 'fileContent.Length' to big-endian:
            int sizeBigEndian = IPAddress.HostToNetworkOrder(fileContent.Length);
            byte[] sizeBytes = BitConverter.GetBytes(sizeBigEndian);

            // 2) The 0xFF 0xFF 0xFF 0xFF marker
            byte[] marker = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

            // 3) ASCII filename + null terminator
            //    e.g. "myfile.gsc" + (byte)0x00
            byte[] fileNameBytes = Encoding.ASCII.GetBytes(fileName);
            byte[] fileNameWithNull = new byte[fileNameBytes.Length + 1];
            Buffer.BlockCopy(fileNameBytes, 0, fileNameWithNull, 0, fileNameBytes.Length);
            fileNameWithNull[fileNameBytes.Length] = 0x00;

            // 4) Put it all together: 
            //    size (4 bytes) + marker (4 bytes) + filenameWithNull + fileContent
            byte[] result = new byte[
                  sizeBytes.Length
                + marker.Length
                + fileNameWithNull.Length
                + fileContent.Length
            ];

            int offset = 0;
            Buffer.BlockCopy(sizeBytes, 0, result, offset, sizeBytes.Length);
            offset += sizeBytes.Length;
            Buffer.BlockCopy(marker, 0, result, offset, marker.Length);
            offset += marker.Length;
            Buffer.BlockCopy(fileNameWithNull, 0, result, offset, fileNameWithNull.Length);
            offset += fileNameWithNull.Length;
            Buffer.BlockCopy(fileContent, 0, result, offset, fileContent.Length);

            return result;
        }

        /// <summary>
        /// Appends a new file entry to the *end* of the decompressed zone file.
        /// </summary>
        /// <param name="zoneFilePath">Full path of the decompressed .zone</param>
        /// <param name="fileName">Filename (should include .gsc, .cfg, etc.)</param>
        /// <param name="fileContent">The raw file bytes you want to inject</param>
        public static void AppendNewRawFile(string zoneFilePath, string fileName, byte[] fileContent)
        {
            // 1) Build the bytes for the new raw file entry
            byte[] newEntryBytes = BuildNewRawFileEntry(fileName, fileContent);

            // 2) Append them at the end
            using (FileStream fs = new FileStream(zoneFilePath, FileMode.Open, FileAccess.Write, FileShare.None))
            {
                // Move to the end of the file
                fs.Seek(0, SeekOrigin.End);

                // Write the new entry
                fs.Write(newEntryBytes, 0, newEntryBytes.Length);
            }
        }
    }
}