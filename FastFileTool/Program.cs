using System.Text;
using Ionic.Zlib;

namespace FastFileTool;

/// <summary>
/// Standalone CLI tool for extracting and packing Call of Duty FastFiles.
/// Supports COD4, WAW, MW2, BO1, MW3, BO2 across PS3, Xbox 360, PC, and Wii.
/// </summary>
class Program
{
    // FastFile header magic bytes
    private static readonly byte[] IWffu100Header = Encoding.ASCII.GetBytes("IWffu100"); // Unsigned
    private static readonly byte[] IWff0100Header = Encoding.ASCII.GetBytes("IWff0100"); // Signed
    private static readonly byte[] TAff0100Header = Encoding.ASCII.GetBytes("TAff0100"); // Treyarch BO2
    private static readonly byte[] S1ff0100Header = Encoding.ASCII.GetBytes("S1ff0100"); // Sledgehammer AW

    // Version bytes for packing (big-endian format)
    private static readonly Dictionary<string, (byte[] Header, byte[] Version)> GameVersions = new()
    {
        // COD4
        { "cod4", (IWffu100Header, new byte[] { 0x00, 0x00, 0x00, 0x01 }) },
        { "cod4-ps3", (IWffu100Header, new byte[] { 0x00, 0x00, 0x00, 0x01 }) },
        { "cod4-x360", (IWffu100Header, new byte[] { 0x00, 0x00, 0x00, 0x01 }) },
        { "cod4-pc", (IWffu100Header, new byte[] { 0x00, 0x00, 0x00, 0x05 }) },
        { "cod4-wii", (IWffu100Header, new byte[] { 0x00, 0x00, 0x01, 0xA2 }) },
        // WAW/COD5
        { "waw", (IWffu100Header, new byte[] { 0x00, 0x00, 0x01, 0x83 }) },
        { "cod5", (IWffu100Header, new byte[] { 0x00, 0x00, 0x01, 0x83 }) },
        { "waw-ps3", (IWffu100Header, new byte[] { 0x00, 0x00, 0x01, 0x83 }) },
        { "waw-x360", (IWffu100Header, new byte[] { 0x00, 0x00, 0x01, 0x83 }) },
        { "waw-pc", (IWffu100Header, new byte[] { 0x00, 0x00, 0x01, 0x83 }) },
        { "waw-wii", (IWffu100Header, new byte[] { 0x00, 0x00, 0x01, 0x9B }) },
        // MW2
        { "mw2", (IWffu100Header, new byte[] { 0x00, 0x00, 0x01, 0x0D }) },
        { "mw2-ps3", (IWffu100Header, new byte[] { 0x00, 0x00, 0x01, 0x0D }) },
        { "mw2-x360", (IWffu100Header, new byte[] { 0x00, 0x00, 0x01, 0x0D }) },
        { "mw2-pc", (IWffu100Header, new byte[] { 0x00, 0x00, 0x01, 0x14 }) },
        // BO1
        { "bo1", (IWffu100Header, new byte[] { 0x00, 0x00, 0x01, 0xD9 }) },
        { "bo1-ps3", (IWffu100Header, new byte[] { 0x00, 0x00, 0x01, 0xD9 }) },
        { "bo1-x360", (IWffu100Header, new byte[] { 0x00, 0x00, 0x01, 0xD9 }) },
        { "bo1-pc", (IWffu100Header, new byte[] { 0x00, 0x00, 0x01, 0xD9 }) },
        { "bo1-wii", (IWffu100Header, new byte[] { 0x00, 0x00, 0x01, 0xDD }) },
        // MW3
        { "mw3", (IWffu100Header, new byte[] { 0x00, 0x00, 0x00, 0x70 }) },
        { "mw3-ps3", (IWffu100Header, new byte[] { 0x00, 0x00, 0x00, 0x70 }) },
        { "mw3-x360", (IWffu100Header, new byte[] { 0x00, 0x00, 0x00, 0x70 }) },
        { "mw3-pc", (IWffu100Header, new byte[] { 0x00, 0x00, 0x00, 0x01 }) },
        { "mw3-wii", (IWffu100Header, new byte[] { 0x00, 0x00, 0x00, 0x6B }) },
        // BO2
        { "bo2", (TAff0100Header, new byte[] { 0x00, 0x00, 0x00, 0x92 }) },
        { "bo2-ps3", (TAff0100Header, new byte[] { 0x00, 0x00, 0x00, 0x92 }) },
        { "bo2-x360", (TAff0100Header, new byte[] { 0x00, 0x00, 0x00, 0x92 }) },
        { "bo2-pc", (TAff0100Header, new byte[] { 0x00, 0x00, 0x00, 0x93 }) },
        { "bo2-wiiu", (TAff0100Header, new byte[] { 0x00, 0x00, 0x00, 0x94 }) },
    };

    // Version detection mapping
    private static readonly Dictionary<uint, (string Game, string[] Platforms)> VersionMap = new()
    {
        { 0x01, ("COD4", new[] { "PS3", "Xbox 360" }) },
        { 0x05, ("COD4", new[] { "PC" }) },
        { 0x1A2, ("COD4", new[] { "Wii" }) },
        { 0x183, ("WAW", new[] { "PS3", "Xbox 360", "PC" }) },
        { 0x19B, ("WAW", new[] { "Wii" }) },
        { 0x10D, ("MW2", new[] { "PS3", "Xbox 360" }) },
        { 0x114, ("MW2", new[] { "PC" }) },
        { 0x1D9, ("BO1", new[] { "PS3", "Xbox 360", "PC" }) },
        { 0x1DD, ("BO1", new[] { "Wii" }) },
        { 0x70, ("MW3", new[] { "PS3", "Xbox 360" }) },
        { 0x6B, ("MW3", new[] { "Wii" }) },
        { 0x92, ("BO2", new[] { "PS3", "Xbox 360" }) },
        { 0x93, ("BO2", new[] { "PC" }) },
        { 0x94, ("BO2", new[] { "Wii U" }) },
        { 0x1D6, ("Quantum of Solace", new[] { "PS3", "Xbox 360", "PC" }) },
        { 0x1D2, ("Quantum of Solace", new[] { "Wii" }) },
    };

    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        string command = args[0].ToLower();

        try
        {
            return command switch
            {
                "extract" or "e" or "-e" => HandleExtract(args),
                "pack" or "p" or "-p" => HandlePack(args),
                "info" or "i" or "-i" => HandleInfo(args),
                "versions" or "v" or "-v" => PrintVersions(),
                "--help" or "-h" or "help" => PrintUsage(),
                _ => PrintUsage()
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    static int PrintUsage()
    {
        Console.WriteLine(@"
FastFile Tool - Extract and Pack COD FastFiles
Supports: COD4, WAW, MW2, BO1, MW3, BO2 | PS3, Xbox 360, PC, Wii

Usage:
  fftool extract <input.ff> [output.zone]
  fftool pack <input.zone> <output.ff> <game-version>
  fftool info <file.ff>
  fftool versions

Commands:
  extract, e    Extract/decompress a .ff file to .zone
  pack, p       Pack/compress a .zone file to .ff
  info, i       Display FastFile header information
  versions, v   List all supported game versions

Examples:
  fftool extract patch_mp.ff
  fftool extract patch_mp.ff myzone.zone
  fftool pack myzone.zone patch_mp.ff cod4
  fftool pack myzone.zone patch_mp.ff waw-pc
  fftool pack myzone.zone patch_mp.ff bo2-ps3
  fftool info patch_mp.ff

Game Versions:
  cod4, cod4-ps3, cod4-x360, cod4-pc, cod4-wii
  waw, waw-ps3, waw-x360, waw-pc, waw-wii (also: cod5)
  mw2, mw2-ps3, mw2-x360, mw2-pc
  bo1, bo1-ps3, bo1-x360, bo1-pc, bo1-wii
  mw3, mw3-ps3, mw3-x360, mw3-pc, mw3-wii
  bo2, bo2-ps3, bo2-x360, bo2-pc, bo2-wiiu
");
        return 0;
    }

    static int PrintVersions()
    {
        Console.WriteLine("\nSupported Game Versions:\n");
        Console.WriteLine("Game        Platform      Version   Header");
        Console.WriteLine("──────────────────────────────────────────────");

        var groups = new[]
        {
            ("COD4", new[] { "cod4-ps3", "cod4-x360", "cod4-pc", "cod4-wii" }),
            ("WAW", new[] { "waw-ps3", "waw-x360", "waw-pc", "waw-wii" }),
            ("MW2", new[] { "mw2-ps3", "mw2-x360", "mw2-pc" }),
            ("BO1", new[] { "bo1-ps3", "bo1-x360", "bo1-pc", "bo1-wii" }),
            ("MW3", new[] { "mw3-ps3", "mw3-x360", "mw3-pc", "mw3-wii" }),
            ("BO2", new[] { "bo2-ps3", "bo2-x360", "bo2-pc", "bo2-wiiu" }),
        };

        foreach (var (game, versions) in groups)
        {
            foreach (var ver in versions)
            {
                if (GameVersions.TryGetValue(ver, out var data))
                {
                    string platform = ver.Contains("-") ? ver.Split('-')[1].ToUpper() : "Console";
                    uint versionInt = (uint)((data.Version[0] << 24) | (data.Version[1] << 16) | (data.Version[2] << 8) | data.Version[3]);
                    string header = Encoding.ASCII.GetString(data.Header);
                    Console.WriteLine($"{game,-11} {platform,-13} 0x{versionInt:X4}    {header}");
                }
            }
            Console.WriteLine();
        }
        return 0;
    }

    static int HandleExtract(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Error: Missing input file path");
            Console.WriteLine("Usage: fftool extract <input.ff> [output.zone]");
            return 1;
        }

        string inputPath = args[1];
        string outputPath = args.Length >= 3 ? args[2] : Path.ChangeExtension(inputPath, ".zone");

        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"Error: File not found: {inputPath}");
            return 1;
        }

        Console.WriteLine($"Extracting: {inputPath}");

        // Show file info first
        ShowFileInfo(inputPath);

        Console.WriteLine($"Output:     {outputPath}");

        int blocks = Decompress(inputPath, outputPath);

        var fi = new FileInfo(outputPath);
        Console.WriteLine($"Blocks:     {blocks}");
        Console.WriteLine($"Done! Extracted {fi.Length:N0} bytes");
        return 0;
    }

    static int HandlePack(string[] args)
    {
        if (args.Length < 4)
        {
            Console.Error.WriteLine("Error: Missing arguments");
            Console.WriteLine("Usage: fftool pack <input.zone> <output.ff> <game-version>");
            Console.WriteLine("Run 'fftool versions' to see available game versions.");
            return 1;
        }

        string inputPath = args[1];
        string outputPath = args[2];
        string gameVersion = args[3].ToLower();

        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"Error: File not found: {inputPath}");
            return 1;
        }

        if (!GameVersions.TryGetValue(gameVersion, out var versionData))
        {
            Console.Error.WriteLine($"Error: Unknown game version '{gameVersion}'");
            Console.WriteLine("Run 'fftool versions' to see available game versions.");
            return 1;
        }

        string headerStr = Encoding.ASCII.GetString(versionData.Header);
        uint versionInt = (uint)((versionData.Version[0] << 24) | (versionData.Version[1] << 16) |
                                  (versionData.Version[2] << 8) | versionData.Version[3]);

        Console.WriteLine($"Packing:    {inputPath}");
        Console.WriteLine($"Output:     {outputPath}");
        Console.WriteLine($"Game:       {gameVersion.ToUpper()}");
        Console.WriteLine($"Header:     {headerStr}");
        Console.WriteLine($"Version:    0x{versionInt:X}");

        int blocks = Compress(inputPath, outputPath, versionData.Header, versionData.Version);

        var fi = new FileInfo(outputPath);
        Console.WriteLine($"Blocks:     {blocks}");
        Console.WriteLine($"Done! Created {fi.Length:N0} byte FastFile");
        return 0;
    }

    static int HandleInfo(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Error: Missing input file path");
            Console.WriteLine("Usage: fftool info <file.ff>");
            return 1;
        }

        string inputPath = args[1];

        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"Error: File not found: {inputPath}");
            return 1;
        }

        ShowFileInfo(inputPath);
        return 0;
    }

    static void ShowFileInfo(string inputPath)
    {
        using var fs = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);

        byte[] header = br.ReadBytes(8);
        byte[] versionBytes = br.ReadBytes(4);

        string headerStr = Encoding.ASCII.GetString(header);

        // Determine header type
        bool isSigned;
        string studio;

        if (headerStr == "IWffu100") { isSigned = false; studio = "Infinity Ward"; }
        else if (headerStr == "IWff0100") { isSigned = true; studio = "Infinity Ward"; }
        else if (headerStr == "TAff0100") { isSigned = true; studio = "Treyarch"; }
        else if (headerStr == "S1ff0100") { isSigned = true; studio = "Sledgehammer"; }
        else { Console.WriteLine($"Unknown header: {headerStr}"); return; }

        uint version = (uint)((versionBytes[0] << 24) | (versionBytes[1] << 16) | (versionBytes[2] << 8) | versionBytes[3]);

        string game = "Unknown";
        string platforms = "Unknown";
        if (VersionMap.TryGetValue(version, out var info))
        {
            game = info.Game;
            platforms = string.Join("/", info.Platforms);
        }

        Console.WriteLine($"File:       {Path.GetFileName(inputPath)}");
        Console.WriteLine($"Size:       {fs.Length:N0} bytes ({fs.Length / 1024.0 / 1024.0:F2} MB)");
        Console.WriteLine($"Header:     {headerStr}");
        Console.WriteLine($"Signed:     {(isSigned ? "Yes (RSA2048)" : "No")}");
        Console.WriteLine($"Studio:     {studio}");
        Console.WriteLine($"Game:       {game}");
        Console.WriteLine($"Platform:   {platforms}");
        Console.WriteLine($"Version:    0x{version:X} ({version})");

        if (isSigned)
            Console.WriteLine("\n[!] Warning: This is a signed FastFile. Modifications will break the signature.");
    }

    static int Decompress(string inputPath, string outputPath)
    {
        using var br = new BinaryReader(new FileStream(inputPath, FileMode.Open, FileAccess.Read), Encoding.Default);
        using var bw = new BinaryWriter(new FileStream(outputPath, FileMode.Create, FileAccess.Write), Encoding.Default);

        br.BaseStream.Position = 12; // Skip header + version

        int blockCount = 0;
        try
        {
            for (int i = 0; i < 5000; i++)
            {
                byte[] lengthBytes = br.ReadBytes(2);
                string lengthHex = BitConverter.ToString(lengthBytes).Replace("-", "");
                int chunkLength = int.Parse(lengthHex, System.Globalization.NumberStyles.AllowHexSpecifier);

                byte[] compressedData = br.ReadBytes(chunkLength);
                byte[] decompressedData = DecompressBlock(compressedData);
                bw.Write(decompressedData);
                blockCount++;
            }
        }
        catch (FormatException) { }
        catch (EndOfStreamException) { }

        return blockCount;
    }

    static int Compress(string inputPath, string outputPath, byte[] headerBytes, byte[] versionBytes)
    {
        using var br = new BinaryReader(new FileStream(inputPath, FileMode.Open, FileAccess.Read), Encoding.Default);
        using var bw = new BinaryWriter(new FileStream(outputPath, FileMode.Create, FileAccess.Write), Encoding.Default);

        bw.Write(headerBytes);
        bw.Write(versionBytes);

        int chunkSize = 65536;
        int blockCount = 0;

        while (br.BaseStream.Position < br.BaseStream.Length)
        {
            byte[] chunk = br.ReadBytes(chunkSize);
            byte[] compressedChunk = CompressBlock(chunk);

            int compressedLength = compressedChunk.Length;
            byte[] lengthBytes = BitConverter.GetBytes(compressedLength);
            Array.Reverse(lengthBytes);
            bw.Write(lengthBytes, 2, 2);

            bw.Write(compressedChunk);
            blockCount++;
        }

        return blockCount;
    }

    static byte[] DecompressBlock(byte[] compressedData)
    {
        using var input = new MemoryStream(compressedData);
        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(input, CompressionMode.Decompress))
        {
            deflate.CopyTo(output);
        }
        return output.ToArray();
    }

    static byte[] CompressBlock(byte[] uncompressedData)
    {
        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionMode.Compress, CompressionLevel.BestCompression))
        {
            deflate.Write(uncompressedData, 0, uncompressedData.Length);
        }
        return output.ToArray();
    }
}
