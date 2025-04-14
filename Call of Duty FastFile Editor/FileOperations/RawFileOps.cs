using Call_of_Duty_FastFile_Editor.Models;
using System;
using System.IO;
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
        ///   Bytes 12 to N: null-terminated filename, then file data.
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

            // The header structure is:
            // - Bytes 0-3: first marker (0xFFFFFFFF)
            // - Bytes 4-7: data size (which we'll update)
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
            int headerLength = fileNameEnd; // The entire header is from offset 0 to fileNameEnd.

            // Extract header.
            byte[] header = new byte[headerLength];
            Array.Copy(entry, header, headerLength);

            // Data portion starts at headerLength.
            int currentDataSize = entry.Length - headerLength;
            byte[] data = new byte[expectedSize];
            if (currentDataSize < expectedSize)
            {
                // Copy available data and pad with zeros.
                Array.Copy(entry, headerLength, data, 0, currentDataSize);
            }
            else
            {
                // Otherwise, take exactly expectedSize bytes.
                Array.Copy(entry, headerLength, data, 0, expectedSize);
            }

            // Update the header’s size field (offset 4, 4 bytes) with the expectedSize (big-endian).
            int newSizeBigEndian = IPAddress.HostToNetworkOrder(expectedSize);
            byte[] newSizeBytes = BitConverter.GetBytes(newSizeBigEndian);
            Array.Copy(newSizeBytes, 0, header, 4, 4);

            // Reassemble and return the adjusted raw file entry.
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
        /// Additionally, it updates the zone file's size (the value at ZoneSizeOffset) by adding
        /// the length of the injected entry. The asset record count is not updated here.
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
                // Extend the file length.
                fs.SetLength(originalLength + newEntryBytes.Length);
                // Shift tail data forward.
                fs.Seek(insertPosition + newEntryBytes.Length, SeekOrigin.Begin);
                fs.Write(tailBuffer, 0, tailBuffer.Length);
                // Write the adjusted new entry.
                fs.Seek(insertPosition, SeekOrigin.Begin);
                fs.Write(newEntryBytes, 0, newEntryBytes.Length);
            });

            // Read the current zone size.
            uint currentZoneSize = Zone.ReadZoneFileSize(zoneFilePath);
            // Add the size of the injected entry.
            uint newZoneSize = currentZoneSize + (uint)newEntryBytes.Length;
            // Write the new size back to the zone file header.
            Zone.WriteZoneFileSize(zoneFilePath, newZoneSize);
            // Also update the in-memory zone header information.
            currentZone.RefreshZoneFileData();
            currentZone.SetZoneOffsets();
        }

        /// <summary>
        /// Adjusts (increases) the size of a raw file node by padding with 0x00 bytes.
        /// This method updates the raw file header’s size field, shifts down the tail data,
        /// writes the new (padded) content into the zone file, and adjusts the overall zone file size.
        /// </summary>
        /// <param name="zoneFilePath">The full path to the decompressed zone file.</param>
        /// <param name="rawFileNode">The RawFileNode to be adjusted.</param>
        /// <param name="newSize">
        /// The desired new size for the file’s data portion.
        /// Must be greater than the current (rawFileNode.MaxSize) size.
        /// </param>
        public static void AdjustRawFileNodeSize(string zoneFilePath, RawFileNode rawFileNode, int newSize)
        {
            int oldSize = rawFileNode.MaxSize;
            if (newSize <= oldSize)
            {
                MessageBox.Show("The new size must be greater than the current size.",
                                "Invalid Size", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int sizeIncrease = newSize - oldSize;

            // Create the new content: copy the existing data then pad with zeros.
            byte[] currentContent = rawFileNode.RawFileBytes;
            byte[] newContent = new byte[newSize];
            Array.Copy(currentContent, newContent, currentContent.Length);
            // The remaining bytes in newContent are already 0.

            Zone currentZone = RawFileNode.CurrentZone;
            currentZone.ModifyZoneFile(fs =>
            {
                // Calculate where the raw file's data ends; that is,
                // the starting point from which we need to shift subsequent bytes.
                long shiftStart = rawFileNode.CodeStartPosition + oldSize;
                long tailLength = fs.Length - shiftStart;

                // If there's any tail data after the raw file entry,
                // shift it further down by sizeIncrease bytes.
                if (tailLength > 0)
                {
                    fs.Seek(shiftStart, SeekOrigin.Begin);
                    byte[] tailData = new byte[tailLength];
                    fs.Read(tailData, 0, tailData.Length);
                    fs.Seek(shiftStart + sizeIncrease, SeekOrigin.Begin);
                    fs.Write(tailData, 0, tailData.Length);
                }

                // Overwrite the raw file's data block (starting at CodeStartPosition)
                // with our new content (which is padded with zeros).
                fs.Seek(rawFileNode.CodeStartPosition, SeekOrigin.Begin);
                fs.Write(newContent, 0, newContent.Length);

                // Update the size in the raw file header.
                // The header is expected to have a 4-byte size field at offset 4 from the start.
                fs.Seek(rawFileNode.StartOfFileHeader + 4, SeekOrigin.Begin);
                int newSizeBigEndian = IPAddress.HostToNetworkOrder(newSize);
                byte[] newSizeBytes = BitConverter.GetBytes(newSizeBigEndian);
                fs.Write(newSizeBytes, 0, newSizeBytes.Length);
            });

            // Update in-memory RawFileNode properties.
            rawFileNode.MaxSize = newSize;
            rawFileNode.RawFileBytes = newContent;
            rawFileNode.RawFileContent = Encoding.Default.GetString(newContent);

            // Update the zone file size header.
            uint currentZoneSize = Zone.ReadZoneFileSize(zoneFilePath);
            uint updatedZoneSize = currentZoneSize + (uint)sizeIncrease;
            Zone.WriteZoneFileSize(zoneFilePath, updatedZoneSize);
        }
    }
}
