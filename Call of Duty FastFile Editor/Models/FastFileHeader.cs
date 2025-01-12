using System;
using System.IO;
using System.Net;
using System.Text;

namespace Call_of_Duty_FastFile_Editor.Models
{
    public class FastFileHeader
    {
        // Properties representing header details
        public string FastFileMagic { get; private set; } // Represents a signed or unsigned FF IWffu100/IWff0100
        public int GameVersion { get; private set; }
        //public short Identifier { get; private set;  // delete eventually, not needed
        public int FileLength { get; private set; }
        public bool IsValid { get; private set; }
        public bool IsCod4File { get; private set; }
        public bool IsCod5File { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FastFileHeader"/> class by reading the header from the specified file.
        /// </summary>
        /// <param name="filePath">The path to the Fast File (.ff).</param>
        public FastFileHeader(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"The file '{filePath}' does not exist.", filePath);

            ReadFastFileHeader(filePath);
        }

        /// <summary>
        /// Reads and parses the Fast File header.
        /// </summary>
        /// <param name="filePath">The path to the Fast File (.ff).</param>
        private void ReadFastFileHeader(string filePath)
        {
            using (BinaryReader binaryReader = new BinaryReader(new FileStream(filePath, FileMode.Open, FileAccess.Read), Encoding.Default))
            {
                // Ensure the file has enough bytes for the header
                if (binaryReader.BaseStream.Length < 14) // 8 + 4 + 2 bytes
                {
                    IsValid = false;
                    return;
                }

                // Read the first 8 characters to determine the file type
                char[] headerChars = binaryReader.ReadChars(8);
                FastFileMagic = new string(headerChars).TrimEnd('\0'); // Remove any trailing null characters

                // Read the next 4 bytes as an integer and convert from network byte order (big-endian) to host byte order (little-endian)
                GameVersion = IPAddress.NetworkToHostOrder(binaryReader.ReadInt32());

                // Not sure if this Identifier is needed
                // Read the next 2 bytes as a short integer and convert from network byte order (big-endian) to host byte order (little-endian)
                //Identifier = IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());

                // Get the length of the file
                FileLength = Convert.ToInt32(new FileInfo(filePath).Length);

                // Validate the header based on specific criteria
                ValidateHeader();
            }
        }

        /// <summary>
        /// Validates the Fast File header.
        /// </summary>
        private void ValidateHeader()
        {
            // Initial validation
            IsValid = false;
            IsCod4File = false;
            IsCod5File = false;

            // Check for specific invalid identifier
            // Not sure if this Identifier is needed
            /*if (Identifier == 30938)
            {
                IsValid = false;
                return;
            }*/

            // Check the FastFileMagic and GameVersion to determine validity
            if (FastFileMagic == Constants.FastFiles.UnSignedFF)
            {
                switch (GameVersion)
                {
                    case Constants.FastFiles.VersionValueCoD4:
                        IsCod4File = true;
                        IsValid = true;
                        break;
                    case Constants.FastFiles.VersionValueWaW:
                        IsCod5File = true;
                        IsValid = true;
                        break;
                    default:
                        IsValid = false;
                        break;
                }
            }
        }
    }
}
