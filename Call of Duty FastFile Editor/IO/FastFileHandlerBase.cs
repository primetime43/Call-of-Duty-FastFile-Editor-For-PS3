using Call_of_Duty_FastFile_Editor.Models;
using System.IO;
using System.Text;
using Ionic.Zlib;

namespace Call_of_Duty_FastFile_Editor.IO
{
    public abstract class FastFileHandlerBase : IFastFileHandler
    {
        protected abstract byte[] HeaderBytes { get; }
        protected abstract byte[] VersionBytes { get; }

        public void Decompress(string inputFilePath, string outputFilePath)
        {
            using (BinaryReader binaryReader = new BinaryReader(new FileStream(inputFilePath, FileMode.Open, FileAccess.Read), Encoding.Default))
            using (BinaryWriter binaryWriter = new BinaryWriter(new FileStream(outputFilePath, FileMode.Create, FileAccess.Write), Encoding.Default))
            {
                binaryReader.BaseStream.Position = HeaderBytes.Length + VersionBytes.Length;

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
                        throw;
                }
            }
        }

        public void Recompress(string ffFilePath, string zoneFilePath, FastFile openedFastFile)
        {
            using (BinaryReader binaryReader = new BinaryReader(new FileStream(zoneFilePath, FileMode.Open, FileAccess.Read), Encoding.Default))
            using (BinaryWriter binaryWriter = new BinaryWriter(new FileStream(ffFilePath, FileMode.Create, FileAccess.Write), Encoding.Default))
            {
                // Write header and version value
                binaryWriter.Write(HeaderBytes);
                binaryWriter.Write(VersionBytes);

                int chunkSize = 65536;
                while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
                {
                    byte[] chunk = binaryReader.ReadBytes(chunkSize);
                    byte[] compressedChunk = CompressFF(chunk);

                    int compressedLength = compressedChunk.Length;
                    byte[] lengthBytes = BitConverter.GetBytes(compressedLength);
                    Array.Reverse(lengthBytes); // Ensure correct byte order
                    binaryWriter.Write(lengthBytes, 2, 2); // Write only the last 2 bytes

                    binaryWriter.Write(compressedChunk);
                }
                binaryWriter.Write(new byte[2] { 0, 1 });
            }
        }

        protected virtual byte[] DecompressFF(byte[] compressedData)
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

        protected virtual byte[] CompressFF(byte[] uncompressedData)
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
