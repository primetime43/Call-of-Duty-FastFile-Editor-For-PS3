using FastFileCompiler;
using FastFileCompiler.Models;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 0;
        }

        try
        {
            return ProcessCommand(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    static void PrintUsage()
    {
        Console.WriteLine("FastFile Compiler for PS3 Call of Duty");
        Console.WriteLine("======================================");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  FastFileCompiler compile <game> <output.ff> [options]");
        Console.WriteLine("  FastFileCompiler example <game> <output.ff>");
        Console.WriteLine();
        Console.WriteLine("Games:");
        Console.WriteLine("  cod4  - Call of Duty 4: Modern Warfare");
        Console.WriteLine("  waw   - Call of Duty: World at War");
        Console.WriteLine("  mw2   - Call of Duty: Modern Warfare 2");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --folder <path>    Add all files from a folder as raw files");
        Console.WriteLine("  --file <path>      Add a single file as a raw file");
        Console.WriteLine("  --name <name>      Set the asset name for the previous --file");
        Console.WriteLine("  --str <path>       Add localized strings from a .str file");
        Console.WriteLine("  --zone-name <name> Set the zone name (default: custom_patch_mp)");
        Console.WriteLine("  --save-zone        Also save the uncompressed .zone file");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  FastFileCompiler compile cod4 patch.ff --folder ./rawfiles");
        Console.WriteLine("  FastFileCompiler compile waw custom.ff --file script.gsc --name maps/mp/test.gsc");
        Console.WriteLine("  FastFileCompiler example cod4 test.ff");
    }

    static int ProcessCommand(string[] args)
    {
        string command = args[0].ToLower();

        switch (command)
        {
            case "compile":
                return CompileCommand(args.Skip(1).ToArray());
            case "example":
                return ExampleCommand(args.Skip(1).ToArray());
            case "help":
            case "--help":
            case "-h":
                PrintUsage();
                return 0;
            default:
                Console.Error.WriteLine($"Unknown command: {command}");
                PrintUsage();
                return 1;
        }
    }

    static int CompileCommand(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: FastFileCompiler compile <game> <output.ff> [options]");
            return 1;
        }

        GameVersion gameVersion = ParseGameVersion(args[0]);
        string outputPath = args[1];
        string zoneName = "custom_patch_mp";
        bool saveZone = false;

        var builder = new ZoneBuilder(gameVersion, zoneName);
        string? pendingFileName = null;

        // Parse options
        for (int i = 2; i < args.Length; i++)
        {
            string arg = args[i].ToLower();

            switch (arg)
            {
                case "--folder":
                    if (++i >= args.Length) throw new ArgumentException("--folder requires a path");
                    AddFilesFromFolder(builder, args[i]);
                    break;

                case "--file":
                    if (++i >= args.Length) throw new ArgumentException("--file requires a path");
                    pendingFileName = args[i];
                    // Check if next arg is --name
                    if (i + 2 < args.Length && args[i + 1].ToLower() == "--name")
                    {
                        string assetName = args[i + 2];
                        builder.AddRawFile(RawFile.FromFile(pendingFileName, assetName));
                        i += 2;
                    }
                    else
                    {
                        builder.AddRawFile(RawFile.FromFile(pendingFileName));
                    }
                    pendingFileName = null;
                    break;

                case "--str":
                    if (++i >= args.Length) throw new ArgumentException("--str requires a path");
                    string strContent = File.ReadAllText(args[i]);
                    builder.AddLocalizedFromStr(strContent);
                    break;

                case "--zone-name":
                    if (++i >= args.Length) throw new ArgumentException("--zone-name requires a name");
                    zoneName = args[i];
                    // Rebuild builder with new zone name
                    builder = new ZoneBuilder(gameVersion, zoneName);
                    break;

                case "--save-zone":
                    saveZone = true;
                    break;

                default:
                    Console.WriteLine($"Warning: Unknown option '{arg}' ignored");
                    break;
            }
        }

        // Compile
        var compiler = new Compiler(gameVersion);
        byte[] zoneData = builder.Build();
        compiler.CompileToFile(zoneData, outputPath, saveZone);

        Console.WriteLine($"Successfully compiled FastFile: {outputPath}");
        Console.WriteLine($"  Game: {gameVersion}");
        Console.WriteLine($"  Zone size: {zoneData.Length:N0} bytes");
        Console.WriteLine($"  Output size: {new FileInfo(outputPath).Length:N0} bytes");

        if (saveZone)
        {
            Console.WriteLine($"  Zone file: {Path.ChangeExtension(outputPath, ".zone")}");
        }

        return 0;
    }

    static int ExampleCommand(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: FastFileCompiler example <game> <output.ff>");
            return 1;
        }

        GameVersion gameVersion = ParseGameVersion(args[0]);
        string outputPath = args[1];

        // Create an example FastFile with a simple GSC script
        var builder = new ZoneBuilder(gameVersion, "example_patch_mp");

        // Add an example GSC file
        string exampleScript = @"// Example GSC Script
// This is a test raw file

init()
{
    level thread onPlayerConnect();
}

onPlayerConnect()
{
    for(;;)
    {
        level waittill(""connected"", player);
        player thread onPlayerSpawned();
    }
}

onPlayerSpawned()
{
    self endon(""disconnect"");
    for(;;)
    {
        self waittill(""spawned_player"");
        self iPrintLnBold(""^2Welcome to the server!"");
    }
}
";
        builder.AddRawFile(RawFile.FromString("maps/mp/gametypes/_example.gsc", exampleScript));

        // Add example localized strings
        builder.AddLocalizedEntry(new LocalizedEntry("EXAMPLE_WELCOME", "Welcome to the example mod!"));
        builder.AddLocalizedEntry(new LocalizedEntry("EXAMPLE_GOODBYE", "Thanks for playing!"));

        // Compile
        var compiler = new Compiler(gameVersion);
        compiler.CompileFromBuilderToFile(builder, outputPath, saveZone: true);

        Console.WriteLine($"Successfully created example FastFile: {outputPath}");
        Console.WriteLine($"  Game: {gameVersion}");
        Console.WriteLine($"  Contains: 1 GSC script, 2 localized strings");
        Console.WriteLine($"  Zone file also saved: {Path.ChangeExtension(outputPath, ".zone")}");

        return 0;
    }

    static void AddFilesFromFolder(ZoneBuilder builder, string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Folder not found: {folderPath}");
        }

        var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
        int count = 0;

        foreach (var filePath in files)
        {
            // Skip .str files (they're for localization)
            if (Path.GetExtension(filePath).ToLower() == ".str")
            {
                string strContent = File.ReadAllText(filePath);
                builder.AddLocalizedFromStr(strContent);
                Console.WriteLine($"  Added localization: {Path.GetFileName(filePath)}");
                continue;
            }

            // Convert path to asset name (relative path with forward slashes)
            string assetName = Path.GetRelativePath(folderPath, filePath).Replace('\\', '/');
            builder.AddRawFile(RawFile.FromFile(filePath, assetName));
            count++;
        }

        Console.WriteLine($"Added {count} raw files from {folderPath}");
    }

    static GameVersion ParseGameVersion(string input)
    {
        return input.ToLower() switch
        {
            "cod4" or "mw" or "mw1" => GameVersion.CoD4,
            "waw" or "cod5" => GameVersion.WaW,
            "mw2" or "cod6" => GameVersion.MW2,
            _ => throw new ArgumentException($"Unknown game version: {input}. Use: cod4, waw, or mw2")
        };
    }
}
