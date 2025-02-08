using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Call_of_Duty_FastFile_Editor.Models;

namespace Call_of_Duty_FastFile_Editor.IO
{
    public static class FastFileProcessing
    {
        /// <summary>
        /// Decompress the FastFile to get a zone file.
        /// </summary>
        /// <param name="inputFilePath"></param>
        /// <param name="outputFilePath"></param>
        public static void DecompressFastFile(string inputFilePath, string outputFilePath)
        {
            using (BinaryReader binaryReader = new BinaryReader(new FileStream(inputFilePath, FileMode.Open, FileAccess.Read), Encoding.Default))
            using (BinaryWriter binaryWriter = new BinaryWriter(new FileStream(outputFilePath, FileMode.Create, FileAccess.Write), Encoding.Default))
            {
                binaryReader.BaseStream.Position = Constants.FastFiles.HeaderLength;

                try
                {
                    for (int i = 1; i < 5000; i++)
                    {
                        byte[] array = binaryReader.ReadBytes(2);
                        string text = BitConverter.ToString(array).Replace("-", "");
                        int count = int.Parse(text, System.Globalization.NumberStyles.AllowHexSpecifier);
                        byte[] compressedData = binaryReader.ReadBytes(count);
                        byte[] decompressedData = FastFileDecompressor.DecompressFF(compressedData);
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
        /// </summary>
        /// <param name="ffFilePath"></param>
        /// <param name="zoneFilePath"></param>
        /// <param name="openedFastFile"></param>
        public static void RecompressFastFile(string ffFilePath, string zoneFilePath, FastFile openedFastFile)
        {
            using (BinaryReader binaryReader = new BinaryReader(new FileStream(zoneFilePath, FileMode.Open, FileAccess.Read), Encoding.Default))
            using (BinaryWriter binaryWriter = new BinaryWriter(new FileStream(ffFilePath, FileMode.Create, FileAccess.Write), Encoding.Default))
            {
                // Write header to the new file
                binaryWriter.Write(Constants.FastFiles.IWffu100_header);

                if (openedFastFile.IsCod5File)
                {
                    binaryWriter.Write(Constants.FastFiles.WaW_VersionValue);
                }
                else if (openedFastFile.IsCod4File)
                {
                    binaryWriter.Write(Constants.FastFiles.CoD4_VersionValue);
                }

                // Set the reader position to skip the header already written
                // This should eventually be removed. This caused a bug where the header would be missing
                // Cause bug #6 https://github.com/primetime43/Call-of-Duty-FastFile-Editor-For-PS3/issues/6
                //binaryReader.BaseStream.Position = Constants.FastFiles.HeaderLength; // Skip the header section

                int chunkSize = 65536;
                while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
                {
                    // Read a chunk
                    byte[] chunk = binaryReader.ReadBytes(chunkSize);

                    // Compress the chunk
                    byte[] compressedChunk = FastFileCompressor.CompressFF(chunk);

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
    }
}