using Ionic.Zlib;

namespace Call_of_Duty_FastFile_Editor.IO
{
    public static class FastFileCompressor
    {
        public static byte[] CompressFF(byte[] uncompressedData)
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