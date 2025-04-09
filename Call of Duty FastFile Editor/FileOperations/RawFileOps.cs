using Call_of_Duty_FastFile_Editor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Call_of_Duty_FastFile_Editor.FileOperations
{
    public class RawFileOps
    {
        // Not sure if this is needed. Should probably use the Save in the SaveRawFile class.

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
            if (newContent.Length > rawFileNode.MaxSize)
            {
                throw new ArgumentException($"New content size ({newContent.Length} bytes) exceeds the maximum allowed size ({rawFileNode.MaxSize} bytes) for file '{rawFileNode.FileName}'.");
            }

            try
            {
                // Get the current Zone instance.
                Zone currentZone = RawFileNode.CurrentZone;

                // Modify the zone file using the helper so that the in-memory data is refreshed automatically.
                currentZone.ModifyZoneFile(fs =>
                {
                    fs.Seek(rawFileNode.CodeStartPosition, SeekOrigin.Begin);
                    fs.Write(newContent, 0, newContent.Length);

                    // Pad with zeros if newContent is smaller than MaxSize.
                    if (newContent.Length < rawFileNode.MaxSize)
                    {
                        byte[] padding = new byte[rawFileNode.MaxSize - newContent.Length];
                        fs.Write(padding, 0, padding.Length);
                    }
                });

                // Update the RawFileNode in memory.
                rawFileNode.RawFileBytes = newContent;
                rawFileNode.RawFileContent = Encoding.Default.GetString(newContent);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new IOException($"Failed to update content for raw file '{rawFileNode.FileName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Increases the size of the raw file entry within the zone file by shifting data,
        /// writing the new content, updating the raw file node’s MaxSize,
        /// and then increasing the zone file header’s size by the same delta.
        /// </summary>
        /// <param name="zoneFilePath">Full path of the decompressed zone file.</param>
        /// <param name="rawFileNode">The raw file node to be expanded.</param>
        /// <param name="newContent">The new raw file content as a byte array.</param>
        public static void IncreaseSize(string zoneFilePath, RawFileNode rawFileNode, byte[] newContent)
        {
            int oldSize = rawFileNode.MaxSize;
            int newSize = newContent.Length;

            // If the new size is not greater, update in-place.
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
                    // Read the data to be shifted
                    fs.Seek(shiftStart, SeekOrigin.Begin);
                    byte[] buffer = new byte[bytesToShift];
                    fs.Read(buffer, 0, buffer.Length);

                    // Write it back starting at the new shifted position
                    fs.Seek(shiftStart + sizeIncrease, SeekOrigin.Begin);
                    fs.Write(buffer, 0, buffer.Length);
                }

                // Write the new raw file content into its position.
                fs.Seek(rawFileNode.CodeStartPosition, SeekOrigin.Begin);
                fs.Write(newContent, 0, newSize);
            });

            // Update the RawFileNode properties in memory.
            rawFileNode.MaxSize = newSize;
            rawFileNode.RawFileBytes = newContent;
            rawFileNode.RawFileContent = Encoding.Default.GetString(newContent);

            // Update the zone file header’s size.
            uint currentZoneSize = Zone.ReadZoneFileSize(zoneFilePath);
            uint newZoneSize = currentZoneSize + (uint)sizeIncrease;
            Zone.WriteZoneFileSize(zoneFilePath, newZoneSize);
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
        /// This matches the format expected by ExtractAllRawFilesSizeAndName.
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
        /// Appends a new file entry to the *start* of the decompressed zone file.
        /// Right at the end of the asset pool.
        /// </summary>
        /// <param name="zoneFilePath">Full path of the decompressed .zone</param>
        /// <param name="fileName">Filename (should include .gsc, .cfg, etc.)</param>
        /// <param name="fileContent">The raw file bytes you want to inject</param>
        public static void AppendNewRawFile(string zoneFilePath, string fileName, byte[] fileContent)
        {
            byte[] newEntryBytes = BuildNewRawFileEntry(fileName, fileContent);
            Zone currentZone = RawFileNode.CurrentZone;

            // Use the AssetPoolEndOffset as the insertion point.
            int insertPosition = currentZone.AssetPoolEndOffset;

            currentZone.ModifyZoneFile(fs =>
            {
                long originalLength = fs.Length;

                // 1) Read everything from insertPosition to the end (the "tail").
                fs.Seek(insertPosition, SeekOrigin.Begin);
                byte[] tailBuffer = new byte[originalLength - insertPosition];
                fs.Read(tailBuffer, 0, tailBuffer.Length);

                // 2) Extend the file length to accommodate the new entry.
                fs.SetLength(originalLength + newEntryBytes.Length);

                // 3) Shift the tail data forward by the size of the new entry.
                fs.Seek(insertPosition + newEntryBytes.Length, SeekOrigin.Begin);
                fs.Write(tailBuffer, 0, tailBuffer.Length);

                // 4) Write the new raw file entry at the insertion point.
                fs.Seek(insertPosition, SeekOrigin.Begin);
                fs.Write(newEntryBytes, 0, newEntryBytes.Length);

                // 5) Update the asset record count in the header (if required).
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

                // 6) Write the termination marker at the new end of the asset pool.
                long newAssetPoolEnd = insertPosition + newEntryBytes.Length + tailBuffer.Length;
                fs.Seek(newAssetPoolEnd, SeekOrigin.Begin);
                byte[] terminationMarker = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                fs.Write(terminationMarker, 0, terminationMarker.Length);
            });

            // Update the in-memory AssetPoolEndOffset by adding the new entry's length.
            currentZone.AssetPoolEndOffset += newEntryBytes.Length;
            // Re-parse the asset pool so that the in-memory records and offsets are updated.
            currentZone.GetSetZoneAssetPool();
        }
    }
}
