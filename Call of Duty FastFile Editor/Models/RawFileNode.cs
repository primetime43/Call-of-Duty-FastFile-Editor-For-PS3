using Call_of_Duty_FastFile_Editor.Models;
using System.Net;
using System.Text;

namespace Call_of_Duty_FastFile_Editor.Models
{
    /// <summary>
    /// Represents a raw file stored within a Fast File, containing metadata and content.
    /// </summary>
    public class RawFileNode
    {
        /// <summary>
        /// Generates the raw file header consisting of the file size (big-endian) and padding (FF FF FF FF).
        /// </summary>
        public byte[] Header
        {
            get
            {
                // Convert MaxSize to a big-endian byte array
                byte[] maxSizeBytes = BitConverter.GetBytes(MaxSize);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(maxSizeBytes);
                }

                // Create the header array with max size and padding
                byte[] header = new byte[8];
                Array.Copy(maxSizeBytes, 0, header, 0, 4); // Copy the first 4 bytes for max size
                for (int i = 4; i < 8; i++)
                {
                    header[i] = 0xFF; // Set the last 4 bytes to FF
                }

                return header;
            }
        }

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
        public string MaxSizeHex => IntToHexBigEndian(MaxSize);

        /// <summary>
        /// Position where the file header starts. 
        /// This is also where the size of the file is located.
        /// </summary>
        public int StartOfFileHeader { get; set; }

        /// <summary>
        /// Gets the position where the code starts, calculated as StartOfFileHeader + 8 + FileName.Length + 1.
        /// </summary>
        public int CodeStartPosition
        {
            get => StartOfFileHeader + 8 + (FileName?.Length ?? 0) + 1;
        }

        /// <summary>
        /// The name of the file.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets the position where the code ends, calculated as CodeStartPosition + MaxSize.
        /// </summary>
        public int CodeEndPosition => CodeStartPosition + MaxSize;

        /// <summary>
        /// The content of the file as a string.
        /// </summary>
        public string RawFileContent { get; set; }

        /// <summary>
        /// The content of the file as a byte array.
        /// </summary>
        public byte[] RawFileBytes { get; set; }

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

        private static string IntToHexBigEndian(int value)
        {
            // Convert the int to a byte array (little-endian by default)
            byte[] bytes = BitConverter.GetBytes(value);

            // Reverse the byte array to switch to big-endian
            Array.Reverse(bytes);

            // Convert the byte array to a hexadecimal string
            return BitConverter.ToString(bytes).Replace("-", "");
        }
    }
}
