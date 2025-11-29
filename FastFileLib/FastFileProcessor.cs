using System.IO.Compression;
using System.Text;

namespace FastFileLib;

/// <summary>
/// Handles FastFile compression and decompression for all supported games.
/// </summary>
public static class FastFileProcessor
{
    private const int BlockSize = 0x10000; // 64KB blocks

    /// <summary>
    /// Decompresses a FastFile to a zone file.
    /// </summary>
    /// <param name="inputPath">Path to the .ff file</param>
    /// <param name="outputPath">Path to output the .zone file</param>
    /// <returns>Number of blocks decompressed</returns>
    public static int Decompress(string inputPath, string outputPath)
    {
        using var br = new BinaryReader(new FileStream(inputPath, FileMode.Open, FileAccess.Read), Encoding.Default);
        using var bw = new BinaryWriter(new FileStream(outputPath, FileMode.Create, FileAccess.Write), Encoding.Default);

        // Read header info
        var info = FastFileInfo.FromReader(br);

        // Skip to compressed data based on game version
        SkipToCompressedData(br, info);

        int blockCount = 0;
        try
        {
            for (int i = 0; i < 5000; i++)
            {
                byte[] lengthBytes = br.ReadBytes(2);
                if (lengthBytes.Length < 2) break;

                int chunkLength = (lengthBytes[0] << 8) | lengthBytes[1];
                if (chunkLength == 0 || chunkLength == 1) break;

                byte[] compressedData = br.ReadBytes(chunkLength);
                if (compressedData.Length < chunkLength) break;

                byte[] decompressedData = DecompressBlock(compressedData);
                bw.Write(decompressedData);
                blockCount++;
            }
        }
        catch (Exception)
        {
            // End of file or invalid data
        }

        return blockCount;
    }

    /// <summary>
    /// Compresses a zone file to a FastFile.
    /// </summary>
    /// <param name="inputPath">Path to the .zone file</param>
    /// <param name="outputPath">Path to output the .ff file</param>
    /// <param name="gameVersion">Target game version</param>
    /// <param name="platform">Target platform (PS3, PC, Wii, etc.)</param>
    /// <returns>Number of blocks compressed</returns>
    public static int Compress(string inputPath, string outputPath, GameVersion gameVersion, string platform = "PS3")
    {
        using var br = new BinaryReader(new FileStream(inputPath, FileMode.Open, FileAccess.Read), Encoding.Default);
        using var bw = new BinaryWriter(new FileStream(outputPath, FileMode.Create, FileAccess.Write), Encoding.Default);

        // Write header
        bw.Write(FastFileInfo.GetMagicBytes());
        bw.Write(FastFileInfo.GetVersionBytes(gameVersion, platform));

        int blockCount = 0;
        while (br.BaseStream.Position < br.BaseStream.Length)
        {
            byte[] chunk = br.ReadBytes(BlockSize);
            byte[] compressedChunk = CompressBlock(chunk);

            // Write length as 2-byte big-endian
            int compressedLength = compressedChunk.Length;
            bw.Write((byte)(compressedLength >> 8));
            bw.Write((byte)(compressedLength & 0xFF));

            bw.Write(compressedChunk);
            blockCount++;
        }

        return blockCount;
    }

    /// <summary>
    /// Skips the header and positions the reader at the start of compressed data.
    /// </summary>
    private static void SkipToCompressedData(BinaryReader br, FastFileInfo info)
    {
        // Reader is already past magic (8) and version (4) = position 12

        if (info.GameVersion == GameVersion.MW2)
        {
            // MW2 extended header structure:
            // allowOnlineUpdate (1 byte)
            // fileCreationTime (8 bytes)
            // region (4 bytes)
            // entryCount (4 bytes)
            // entries (entryCount * 0x14 bytes for PS3)
            // fileSizes (8 bytes)

            br.ReadByte();           // allowOnlineUpdate
            br.ReadBytes(8);         // fileCreationTime
            br.ReadBytes(4);         // region

            byte[] entryCountBytes = br.ReadBytes(4);
            int entryCount = (entryCountBytes[0] << 24) | (entryCountBytes[1] << 16) |
                            (entryCountBytes[2] << 8) | entryCountBytes[3];

            // Skip entries (each entry is 0x14 = 20 bytes on PS3)
            if (entryCount > 0 && entryCount < 10000) // Sanity check
            {
                br.ReadBytes(entryCount * 0x14);
            }

            br.ReadBytes(8);         // fileSizes
        }
        // For CoD4/WaW, we're already at the correct position (12)
    }

    /// <summary>
    /// Decompresses a single block of data.
    /// Automatically detects zlib header vs raw deflate.
    /// </summary>
    public static byte[] DecompressBlock(byte[] compressedData)
    {
        // Check if data has zlib header (0x78 followed by compression level byte)
        // MW2 uses zlib-wrapped deflate, CoD4/WaW use raw deflate
        bool hasZlibHeader = compressedData.Length >= 2 &&
                             compressedData[0] == 0x78 &&
                             (compressedData[1] == 0x01 || compressedData[1] == 0x5E ||
                              compressedData[1] == 0x9C || compressedData[1] == 0xDA);

        using var input = new MemoryStream(compressedData);
        using var output = new MemoryStream();

        if (hasZlibHeader)
        {
            // Use ZLibStream for zlib-wrapped data (MW2)
            using (var zlib = new ZLibStream(input, CompressionMode.Decompress))
            {
                zlib.CopyTo(output);
            }
        }
        else
        {
            // Use DeflateStream for raw deflate data (CoD4/WaW)
            using (var deflate = new DeflateStream(input, CompressionMode.Decompress))
            {
                deflate.CopyTo(output);
            }
        }
        return output.ToArray();
    }

    /// <summary>
    /// Compresses a single block of data using raw deflate.
    /// </summary>
    public static byte[] CompressBlock(byte[] uncompressedData)
    {
        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal))
        {
            deflate.Write(uncompressedData, 0, uncompressedData.Length);
        }
        return output.ToArray();
    }
}
