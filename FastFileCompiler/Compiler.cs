using System.Text;
using Ionic.Zlib;

namespace FastFileCompiler;

/// <summary>
/// Compiles zone data into a FastFile (.ff) with proper header and ZLIB block compression.
/// </summary>
public class Compiler
{
    private readonly GameVersion _gameVersion;

    public Compiler(GameVersion gameVersion)
    {
        _gameVersion = gameVersion;
    }

    /// <summary>
    /// Compiles zone data into a complete FastFile.
    /// </summary>
    /// <param name="zoneData">The raw zone data from ZoneBuilder.Build()</param>
    /// <returns>The complete FastFile bytes ready to be written to disk.</returns>
    public byte[] Compile(byte[] zoneData)
    {
        var fastFile = new List<byte>();

        // Build FastFile header (12 bytes for CoD4/WaW)
        byte[] header = BuildFastFileHeader();
        fastFile.AddRange(header);

        // Compress zone data in 64KB blocks
        byte[] compressedBlocks = CompressZoneBlocks(zoneData);
        fastFile.AddRange(compressedBlocks);

        // End marker: 00 01
        fastFile.AddRange(new byte[] { 0x00, 0x01 });

        return fastFile.ToArray();
    }

    /// <summary>
    /// Builds the FastFile header.
    /// </summary>
    private byte[] BuildFastFileHeader()
    {
        var header = new List<byte>();

        // Magic: "IWffu100" (8 bytes)
        header.AddRange(Encoding.ASCII.GetBytes(FastFileConstants.UnsignedHeader));

        // Version (4 bytes, big-endian)
        header.AddRange(FastFileConstants.GetVersionBytes(_gameVersion));

        return header.ToArray();
    }

    /// <summary>
    /// Compresses the zone data into 64KB ZLIB blocks.
    /// Each block format: [2-byte length (big-endian)] + [compressed data without ZLIB header]
    /// </summary>
    private byte[] CompressZoneBlocks(byte[] zoneData)
    {
        var compressed = new List<byte>();

        using var reader = new MemoryStream(zoneData);
        int blockCount = (zoneData.Length + FastFileConstants.BlockSize - 1) / FastFileConstants.BlockSize;

        for (int i = 0; i < blockCount; i++)
        {
            // Read up to 64KB
            int bytesToRead = Math.Min(FastFileConstants.BlockSize, (int)(zoneData.Length - reader.Position));
            byte[] block = new byte[bytesToRead];
            reader.Read(block, 0, bytesToRead);

            // Compress the block
            byte[] compressedBlock = ZlibStream.CompressBuffer(block);

            // The compressed data includes a 2-byte ZLIB header (usually 78 DA for best compression)
            // We need to write: [length without header] + [compressed data without first 2 bytes]
            int compressedLength = compressedBlock.Length - 2;

            // Write length as 2-byte big-endian
            compressed.Add((byte)(compressedLength >> 8));
            compressed.Add((byte)(compressedLength & 0xFF));

            // Write compressed data (skip first 2 bytes - the ZLIB header)
            compressed.AddRange(compressedBlock.Skip(2));
        }

        return compressed.ToArray();
    }

    /// <summary>
    /// Compiles zone data and saves to a file.
    /// </summary>
    /// <param name="zoneData">The raw zone data from ZoneBuilder.Build()</param>
    /// <param name="outputPath">The output .ff file path.</param>
    /// <param name="saveZone">If true, also saves the uncompressed zone file.</param>
    public void CompileToFile(byte[] zoneData, string outputPath, bool saveZone = false)
    {
        byte[] fastFile = Compile(zoneData);
        File.WriteAllBytes(outputPath, fastFile);

        if (saveZone)
        {
            string zonePath = Path.ChangeExtension(outputPath, ".zone");
            File.WriteAllBytes(zonePath, zoneData);
        }
    }

    /// <summary>
    /// High-level method to compile from a ZoneBuilder directly.
    /// </summary>
    public byte[] CompileFromBuilder(ZoneBuilder builder)
    {
        byte[] zoneData = builder.Build();
        return Compile(zoneData);
    }

    /// <summary>
    /// High-level method to compile from a ZoneBuilder and save to file.
    /// </summary>
    public void CompileFromBuilderToFile(ZoneBuilder builder, string outputPath, bool saveZone = false)
    {
        byte[] zoneData = builder.Build();
        CompileToFile(zoneData, outputPath, saveZone);
    }
}
