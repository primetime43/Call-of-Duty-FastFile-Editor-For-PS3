using Call_of_Duty_FastFile_Editor.Models;
using Ionic.Zlib;
using System.Text;
using Call_of_Duty_FastFile_Editor.Constants;

namespace Call_of_Duty_FastFile_Editor.IO
{
    public class CoD4FastFileHandler : IFastFileHandler
    {
        // Can eventually move these into the parent class since a lot of overlapping functionality exists


        /// <summary>
        /// Decompress the FastFile to get a zone file.
        /// </summary>
        /// <param name="inputFilePath"></param>
        /// <param name="outputFilePath"></param>
        public void Decompress(string inputFilePath, string outputFilePath)
        {
            using (BinaryReader binaryReader = new BinaryReader(new FileStream(inputFilePath, FileMode.Open, FileAccess.Read), Encoding.Default))
            using (BinaryWriter binaryWriter = new BinaryWriter(new FileStream(outputFilePath, FileMode.Create, FileAccess.Write), Encoding.Default))
            {
                binaryReader.BaseStream.Position = FastFileHeaderConstants.HeaderLength;

                try
                {
                    for (int i = 1; i < 5000; i++)
                    {
                        byte[] array = binaryReader.ReadBytes(2);
                        string text = BitConverter.ToString(array).Replace("-", "");
                        int count = int.Parse(text, System.Globalization.NumberStyles.AllowHexSpecifier);
                        byte[] compressedData = binaryReader.ReadBytes(count);
                        byte[] decompressedData = DecompressFF(compressedData);
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

        /// <summary>
        /// Recompress the extracted zone file back into a FastFile.
        /// Saves any changes to the zone in the FastFile.
        /// </summary>
        /// <param name="ffFilePath"></param>
        /// <param name="zoneFilePath"></param>
        /// <param name="openedFastFile"></param>
        public void Recompress(string ffFilePath, string zoneFilePath, FastFile openedFastFile)
        {
            using (BinaryReader binaryReader = new BinaryReader(new FileStream(zoneFilePath, FileMode.Open, FileAccess.Read), Encoding.Default))
            using (BinaryWriter binaryWriter = new BinaryWriter(new FileStream(ffFilePath, FileMode.Create, FileAccess.Write), Encoding.Default))
            {
                // Write header to the new file
                binaryWriter.Write(FastFileHeaderConstants.IWffu100Header);

                // For CoD4, always use CoD4_VersionValue
                binaryWriter.Write(FastFileHeaderConstants.CoD4VersionValue);

                int chunkSize = 65536;
                while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
                {
                    // Read a chunk
                    byte[] chunk = binaryReader.ReadBytes(chunkSize);

                    // Compress the chunk
                    byte[] compressedChunk = CompressFF(chunk);

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
        /// Decompresses the specified byte array using the Zlib algorithm.
        /// </summary>
        /// <param name="compressedData"></param>
        /// <returns></returns>
        private byte[] DecompressFF(byte[] compressedData)
        {
            using (MemoryStream input = new MemoryStream(compressedData))
            using (MemoryStream output = new MemoryStream())
            {
                using (DeflateStream deflateStream = new DeflateStream(input, CompressionMode.Decompress))
                {
                    deflateStream.CopyTo(output);
                }
                return output.ToArray();
            }
        }

        private byte[] CompressFF(byte[] uncompressedData)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress, CompressionLevel.BestCompression))
                {
                    deflateStream.Write(uncompressedData, 0, uncompressedData.Length);
                }
                return memoryStream.ToArray();
            }
        }
    }
}
