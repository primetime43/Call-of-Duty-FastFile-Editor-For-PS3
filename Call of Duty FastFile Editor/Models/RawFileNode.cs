using Call_of_Duty_FastFile_Editor.Services;
using System;
using System.Net;
using System.Text;

namespace Call_of_Duty_FastFile_Editor.Models
{
    /// <summary>
    /// Represents a raw file stored within a FastFile, containing metadata and content.
    /// </summary>
    public class RawFileNode : IAssetRecordUpdatable
    {
        // Backing field for the header bytes.
        private byte[] _header;

        public RawFileNode() { }

        // Constructor that initializes the necessary properties.
        public RawFileNode(string name, byte[] buffer)
        {
            FileName = name;
            RawFileBytes = buffer;
            MaxSize = buffer.Length;  // Use MaxSize consistently
        }

        /// <summary>
        /// Static property to hold the currently loaded zone.
        /// </summary>
        public static Zone CurrentZone { get; set; }

        public byte[] Header => Utilities.GetBytesAtOffset(StartOfFileHeader, CurrentZone, EndOfFileHeader - StartOfFileHeader);

        /// <summary>
        /// The position where one of the patterns was found in the file.
        /// </summary>
        public int PatternIndexPosition { get; set; }

        /// <summary>
        /// Maximum allowed size for the file's content.
        /// </summary>
        public int MaxSize { get; set; }

        /// <summary>
        /// Maximum allowed size for the file's content in hex format.
        /// Computed as the big-endian representation of MaxSize.
        /// </summary>
        public string MaxSizeHex => Utilities.ConvertToBigEndianHex(MaxSize);

        public int StartOfFileHeader { get; set; }

        /// <summary>
        /// Gets the position where the code starts, calculated as StartOfFileHeader + 12 + FileName.Length + 1.
        /// 12 comes from FF FF FF FF name pointer, 4 bytes for data length, and FF FF FF FF for data pointer
        /// </summary>
        public int CodeStartPosition => StartOfFileHeader + 12 + (FileName?.Length ?? 0) + 1;

        /// <summary>
        /// Where the data ends
        /// Includes null byte
        /// </summary>
        public int EndOfFileHeader => CodeStartPosition - 1;

        /// <summary>
        /// The name of the file.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets the position where the code ends, calculated as CodeStartPosition + MaxSize + 1 for the null byte.
        /// Includes null byte
        /// </summary>
        public int CodeEndPosition => CodeStartPosition + MaxSize + 1;

        /// <summary>
        /// The content of the file as a string.
        /// </summary>
        public string RawFileContent { get; set; }

        /// <summary>
        /// The content of the file as a byte array.
        /// </summary>
        public byte[] RawFileBytes { get; set; }

        public string AdditionalData { get; set; }

        /// <summary>
        /// Updates the file name and returns the byte array representation.
        /// </summary>
        /// <param name="newFileName">The new file name.</param>
        /// <returns>Byte array of the new file name with a null terminator.</returns>
        public byte[] GetFileNameBytes(string newFileName)
        {
            // Convert the new file name to ASCII bytes
            byte[] fileNameBytes = Encoding.ASCII.GetBytes(newFileName);

            // Append a null terminator
            byte[] result = new byte[fileNameBytes.Length + 1];
            Array.Copy(fileNameBytes, result, fileNameBytes.Length);
            result[fileNameBytes.Length] = 0x00;

            return result;
        }

        public void UpdateAssetRecord(ref ZoneAssetRecord assetRecord)
        {
            assetRecord.HeaderStartOffset = this.StartOfFileHeader;
            assetRecord.HeaderEndOffset = this.EndOfFileHeader;
            assetRecord.AssetDataStartPosition = this.CodeStartPosition;
            assetRecord.AssetDataEndOffset = this.CodeEndPosition;
            assetRecord.Name = this.FileName;
            assetRecord.RawDataBytes = this.RawFileBytes;
            assetRecord.Size = this.MaxSize;
            assetRecord.Content = this.RawFileContent;
            assetRecord.AdditionalData = this.AdditionalData;
        }
    }
}
