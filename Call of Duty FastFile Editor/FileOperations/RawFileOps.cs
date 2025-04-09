using Call_of_Duty_FastFile_Editor.Models;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Call_of_Duty_FastFile_Editor.FileOperations
{
    public class RawFileOps
    {
        /// <summary>
        /// Updates the content of a specific file within the zone file.
        /// Overwrites in-place and pads with zeros if needed.
        /// </summary>
        public static void UpdateFileContent(string zoneFilePath, RawFileNode rawFileNode, byte[] newContent)
        {
            if (newContent.Length > rawFileNode.MaxSize)
            {
                throw new ArgumentException($"New content size ({newContent.Length} bytes) exceeds the maximum allowed size ({rawFileNode.MaxSize} bytes) for file '{rawFileNode.FileName}'.");
            }

            try
            {
                Zone currentZone = RawFileNode.CurrentZone;
                currentZone.ModifyZoneFile(fs =>
                {
                    fs.Seek(rawFileNode.CodeStartPosition, SeekOrigin.Begin);
                    fs.Write(newContent, 0, newContent.Length);

                    if (newContent.Length < rawFileNode.MaxSize)
                    {
                        byte[] padding = new byte[rawFileNode.MaxSize - newContent.Length];
                        fs.Write(padding, 0, padding.Length);
                    }
                });

                rawFileNode.RawFileBytes = newContent;
                rawFileNode.RawFileContent = Encoding.Default.GetString(newContent);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new IOException($"Failed to update content for raw file '{rawFileNode.FileName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Increases the size of a raw file entry by shifting tail data, writing new content,
        /// updating the raw file node’s MaxSize, and adjusting the zone file header’s size.
        /// </summary>
        public static void IncreaseSize(string zoneFilePath, RawFileNode rawFileNode, byte[] newContent)
        {
            int oldSize = rawFileNode.MaxSize;
            int newSize = newContent.Length;
            if (newSize <= oldSize)
            {
                UpdateFileContent(zoneFilePath, rawFileNode, newContent);
                return;
            }

            int sizeIncrease = newSize - oldSize;
            Zone currentZone = RawFileNode.CurrentZone;

            currentZone.ModifyZoneFile(fs =>
            {
                long shiftStart = rawFileNode.CodeStartPosition + oldSize;
                long bytesToShift = fs.Length - shiftStart;
                if (bytesToShift > 0)
                {
                    fs.Seek(shiftStart, SeekOrigin.Begin);
                    byte[] buffer = new byte[bytesToShift];
                    fs.Read(buffer, 0, buffer.Length);
                    fs.Seek(shiftStart + sizeIncrease, SeekOrigin.Begin);
                    fs.Write(buffer, 0, buffer.Length);
                }
                fs.Seek(rawFileNode.CodeStartPosition, SeekOrigin.Begin);
                fs.Write(newContent, 0, newSize);
            });

            rawFileNode.MaxSize = newSize;
            rawFileNode.RawFileBytes = newContent;
            rawFileNode.RawFileContent = Encoding.Default.GetString(newContent);

            uint currentZoneSize = Zone.ReadZoneFileSize(zoneFilePath);
            uint newZoneSize = currentZoneSize + (uint)sizeIncrease;
            Zone.WriteZoneFileSize(zoneFilePath, newZoneSize);
        }

        /// <summary>
        /// Adjusts a raw file entry read from disk so that its header’s size field (at offset 4)
        /// matches the expected data size. It uses the known header structure:
        ///   Bytes 0-3: first marker (0xFFFFFFFF)
        ///   Bytes 4-7: data size (to be updated)
        ///   Bytes 8-11: second marker (0xFFFFFFFF)
        ///   Bytes 12 to N: null-terminated filename
        /// Followed by the file data.
        /// The method pads or trims the data portion so that its length equals the expected size.
        /// Finally, it returns the reassembled entry.
        /// </summary>
        /// <param name="filePath">Full path to the file being injected (which already contains its header).</param>
        /// <param name="expectedSize">The expected size for the file’s data portion (RawFileNode.MaxSize).</param>
        /// <returns>An adjusted raw file entry as a byte array.</returns>
        private static byte[] AdjustRawFileEntry(string filePath, int expectedSize)
        {
            // Read the entire file from disk.
            byte[] entry = File.ReadAllBytes(filePath);
            if (entry.Length < 12)
                throw new Exception("File too short to contain a valid header.");

            // The header structure is fixed:
            // - Bytes 0-3: first marker (0xFFFFFFFF)
            // - Bytes 4-7: data size (to update)
            // - Bytes 8-11: second marker (0xFFFFFFFF)
            // - Bytes 12: start of filename (null terminated)
            int fileNameStart = 12;
            int fileNameEnd = fileNameStart;
            while (fileNameEnd < entry.Length && entry[fileNameEnd] != 0x00)
            {
                fileNameEnd++;
            }
            if (fileNameEnd == entry.Length)
                throw new Exception("Filename in header is not null-terminated.");
            fileNameEnd++; // Include the null terminator.
            int headerLength = fileNameEnd; // entire header is from 0 to fileNameEnd.

            // Extract the header.
            byte[] header = new byte[headerLength];
            Array.Copy(entry, header, headerLength);

            // Data portion begins at offset headerLength.
            int currentDataSize = entry.Length - headerLength;
            byte[] data = new byte[expectedSize];
            if (currentDataSize < expectedSize)
            {
                // Copy existing data and pad with zeros.
                Array.Copy(entry, headerLength, data, 0, currentDataSize);
            }
            else
            {
                // Otherwise, take exactly expectedSize bytes.
                Array.Copy(entry, headerLength, data, 0, expectedSize);
            }

            // Update the header’s size field (located at offset 4, 4 bytes).
            int newSizeBigEndian = IPAddress.HostToNetworkOrder(expectedSize);
            byte[] newSizeBytes = BitConverter.GetBytes(newSizeBigEndian);
            // Overwrite bytes 4-7 in the header.
            Array.Copy(newSizeBytes, 0, header, 4, 4);

            // Reassemble the new entry.
            byte[] newEntry = new byte[header.Length + data.Length];
            Buffer.BlockCopy(header, 0, newEntry, 0, header.Length);
            Buffer.BlockCopy(data, 0, newEntry, header.Length, data.Length);

            return newEntry;
        }

        /// <summary>
        /// Appends a new raw file entry to the decompressed zone file at the end of the asset pool.
        /// This method assumes that the file being injected already contains its header.
        /// It reads the entire file from disk, adjusts the header size field (and data portion)
        /// so that the entry’s data length matches expectedSize, and then injects the adjusted entry.
        /// </summary>
        /// <param name="zoneFilePath">Full path of the decompressed zone file.</param>
        /// <param name="filePath">
        /// The full path to the file to be injected (which already contains its header).
        /// </param>
        /// <param name="expectedSize">The expected size for the data portion (RawFileNode.MaxSize).</param>
        public static void AppendNewRawFile(string zoneFilePath, string filePath, int expectedSize)
        {
            // Adjust the raw file entry from disk.
            byte[] newEntryBytes = AdjustRawFileEntry(filePath, expectedSize);
            Zone currentZone = RawFileNode.CurrentZone;
            int insertPosition = currentZone.AssetPoolEndOffset;

            currentZone.ModifyZoneFile(fs =>
            {
                long originalLength = fs.Length;
                // Read tail data from the insertion point.
                fs.Seek(insertPosition, SeekOrigin.Begin);
                byte[] tailBuffer = new byte[originalLength - insertPosition];
                fs.Read(tailBuffer, 0, tailBuffer.Length);
                // Extend file length.
                fs.SetLength(originalLength + newEntryBytes.Length);
                // Shift tail data forward.
                fs.Seek(insertPosition + newEntryBytes.Length, SeekOrigin.Begin);
                fs.Write(tailBuffer, 0, tailBuffer.Length);
                // Write the adjusted new entry.
                fs.Seek(insertPosition, SeekOrigin.Begin);
                fs.Write(newEntryBytes, 0, newEntryBytes.Length);

                // Update the asset record count in the header.
                int assetRecordCountOffset = Constants.ZoneFile.AssetRecordCountOffset;
                fs.Seek(assetRecordCountOffset, SeekOrigin.Begin);
                byte[] countBytes = new byte[4];
                fs.Read(countBytes, 0, countBytes.Length);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(countBytes);
                uint currentCount = BitConverter.ToUInt32(countBytes, 0);
                uint newCount = currentCount + 1;
                byte[] newCountBytes = BitConverter.GetBytes(newCount);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(newCountBytes);
                fs.Seek(assetRecordCountOffset, SeekOrigin.Begin);
                fs.Write(newCountBytes, 0, newCountBytes.Length);

                // Write termination marker at the new end of the asset pool.
                long newAssetPoolEnd = insertPosition + newEntryBytes.Length + tailBuffer.Length;
                fs.Seek(newAssetPoolEnd, SeekOrigin.Begin);
                byte[] terminationMarker = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                fs.Write(terminationMarker, 0, terminationMarker.Length);
            });

            currentZone.AssetPoolEndOffset += newEntryBytes.Length;
            currentZone.GetSetZoneAssetPool();
        }
    }
}
