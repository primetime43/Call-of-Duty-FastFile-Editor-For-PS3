using System;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace Call_of_Duty_FastFile_Editor.IO
{
    public class FastFileHeader
    {
        public string FileType { get; private set; }
        public int FileLength { get; private set; }
        public bool IsValid { get; private set; }
        public bool IsCod4File { get; private set; }
        public bool IsCod5File { get; private set; }

        public FastFileHeader(string filePath)
        {
            ReadFastFileHeader(filePath);
        }

        private void ReadFastFileHeader(string filePath)
        {
            using (BinaryReader binaryReader = new BinaryReader(new FileStream(filePath, FileMode.Open), Encoding.Default))
            {
                // Read the first 8 characters to determine the file type
                char[] headerChars = binaryReader.ReadChars(8);
                FileType = new string(headerChars); //header string 

                // Read the next 4 bytes as an integer and convert from network byte order (big-endian) to host byte order (little-endian)
                int gameVersion = IPAddress.NetworkToHostOrder(binaryReader.ReadInt32());

                // Read the next 2 bytes as a short integer and convert from network byte order (big-endian) to host byte order (little-endian)
                int identifier = IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());

                // Get the length of the file
                FileLength = Convert.ToInt32(new FileInfo(filePath).Length);

                // Validate the header based on specific criteria
                IsValid = false;
                if (identifier == 30938)
                {
                    IsValid = false;
                }
                else if (FileType != "Iwffu100")
                {
                    // 1 = CoD4, 387 = CoD5
                    switch (gameVersion)
                    {
                        case 1:
                            IsCod4File = true;
                            IsValid = true;
                            break;
                        case 387:
                            IsCod5File = true;
                            IsValid = true;
                            break;
                        default:
                            IsValid = false;
                            break;
                    }
                }

                // Show an error message if the file is not valid
                if (!IsValid)
                {
                    MessageBox.Show("Invalid Fast File!\n\nThe Fast File you have selected is not a valid PS3 .ff!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                }
            }
        }
    }
}