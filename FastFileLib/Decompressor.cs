using System.IO.Compression;

namespace FastFileLib;

/// <summary>
/// Decompresses FastFile (.ff) archives into raw zone data.
/// </summary>
public class Decompressor
{
    /// <summary>
    /// Decompresses a FastFile and returns the raw zone data.
    /// </summary>
    /// <param name="ffPath">Path to the FastFile (.ff).</param>
    /// <returns>The decompressed zone data.</returns>
    public byte[] Decompress(string ffPath)
    {
        byte[] ffData = File.ReadAllBytes(ffPath);
        return Decompress(ffData);
    }

    /// <summary>
    /// Decompresses FastFile data and returns the raw zone data.
    /// </summary>
    /// <param name="ffData">The FastFile bytes.</param>
    /// <returns>The decompressed zone data.</returns>
    public byte[] Decompress(byte[] ffData)
    {
        // Skip the 12-byte header (8 magic + 4 version)
        int offset = FastFileConstants.HeaderSize;

        var decompressed = new List<byte>();

        while (offset < ffData.Length - 1)
        {
            // Read 2-byte block size (big-endian)
            int blockSize = (ffData[offset] << 8) | ffData[offset + 1];
            offset += 2;

            // Check for end marker (00 01 or no more data)
            if (blockSize == 0 || blockSize == 1 || offset + blockSize > ffData.Length)
                break;

            // Read compressed block and prepend zlib header for decompression
            // CoD4/WaW store raw deflate data - adding zlib header allows ZLibStream to decompress
            byte[] compressedBlock = new byte[blockSize + 2];
            compressedBlock[0] = 0x78; // zlib header byte 1
            compressedBlock[1] = 0xDA; // zlib header byte 2 (best compression)
            Array.Copy(ffData, offset, compressedBlock, 2, blockSize);
            offset += blockSize;

            // Decompress block using ZLibStream
            byte[] decompressedBlock = DecompressBlockWithZlib(compressedBlock);
            decompressed.AddRange(decompressedBlock);
        }

        byte[] result = decompressed.ToArray();

        // Fix header sizes if they don't match actual decompressed size
        if (result.Length >= 52)
        {
            FixZoneHeaderSizes(result);
        }

        return result;
    }

    /// <summary>
    /// Decompresses a block using ZLibStream.
    /// </summary>
    private static byte[] DecompressBlockWithZlib(byte[] compressedData)
    {
        using var inputStream = new MemoryStream(compressedData);
        using var zlibStream = new ZLibStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();

        zlibStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }

    /// <summary>
    /// Ensures zone header size fields match the actual zone length.
    /// </summary>
    private static void FixZoneHeaderSizes(byte[] zoneData)
    {
        int totalZoneSize = zoneData.Length;
        int totalDataSize = totalZoneSize - 52 + 16;

        // Offset 0-3: Total data size (zone size - header size + 16)
        zoneData[0] = (byte)(totalDataSize >> 24);
        zoneData[1] = (byte)(totalDataSize >> 16);
        zoneData[2] = (byte)(totalDataSize >> 8);
        zoneData[3] = (byte)totalDataSize;

        // Offset 24-27: Total zone size (actual file length)
        zoneData[24] = (byte)(totalZoneSize >> 24);
        zoneData[25] = (byte)(totalZoneSize >> 16);
        zoneData[26] = (byte)(totalZoneSize >> 8);
        zoneData[27] = (byte)totalZoneSize;
    }

    /// <summary>
    /// Decompresses a FastFile and saves the zone data to a file.
    /// </summary>
    /// <param name="ffPath">Path to the FastFile (.ff).</param>
    /// <param name="zonePath">Output path for the zone file.</param>
    public void DecompressToFile(string ffPath, string zonePath)
    {
        byte[] zoneData = Decompress(ffPath);
        File.WriteAllBytes(zonePath, zoneData);
    }
}
