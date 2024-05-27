using Ionic.Zlib;

namespace Call_of_Duty_FastFile_Editor.IO
{
    public static class FastFileDecompressor
    {
        public static byte[] DecompressFF(byte[] compressedData)
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
    }
}