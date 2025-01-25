using Call_of_Duty_FastFile_Editor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Call_of_Duty_FastFile_Editor.FileOperations
{
    public class RawFileInject
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

        // Maybe move this elsewhere

        /// <summary>
        /// Constructs a byte array that contains:
        /// 
        ///   [ 4-byte big-endian size ]
        ///   [ 0xFF 0xFF 0xFF 0xFF ]
        ///   [ ASCII filename + 0x00 ]
        ///   [ raw content bytes (size) ]
        /// 
        /// This matches the format expected by ExtractZoneFileEntriesWithSizeAndName.
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
