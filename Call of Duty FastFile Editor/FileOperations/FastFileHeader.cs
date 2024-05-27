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

        public FastFileHeader(string filePath)
        {
            ReadFastFileHeader(filePath);
        }

        private void ReadFastFileHeader(string filePath)
        {
            using (BinaryReader binaryReader = new BinaryReader(new FileStream(filePath, FileMode.Open), Encoding.Default))
            {
                // Read the first 8 characters to determine the file type
                char[] value = binaryReader.ReadChars(8);
                FileType = new string(value);

                // Read the next 4 bytes as an integer and convert from network byte order (big-endian) to host byte order (little-endian)
                int num = IPAddress.NetworkToHostOrder(binaryReader.ReadInt32());

                // Read the next 2 bytes as a short integer and convert from network byte order (big-endian) to host byte order (little-endian)
                int num2 = IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());

                // Get the length of the file
                FileLength = Convert.ToInt32(new FileInfo(filePath).Length);

                // Validate the header based on specific criteria
                IsValid = false;
                if (num2 == 30938)
                {
                    IsValid = false;
                }
                else if (FileType != "Iwffu100")
                {
                    if (num == 1 || num == 387)
                    {
                        IsValid = true;
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