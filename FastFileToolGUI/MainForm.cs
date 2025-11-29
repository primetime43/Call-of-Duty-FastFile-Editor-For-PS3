using System.Text;
using System.IO.Compression;

namespace FastFileToolGUI;

public partial class MainForm : Form
{
    // FastFile header magic bytes
    private static readonly byte[] IWffu100Header = Encoding.ASCII.GetBytes("IWffu100"); // Unsigned
    private static readonly byte[] IWff0100Header = Encoding.ASCII.GetBytes("IWff0100"); // Signed
    private static readonly byte[] TAff0100Header = Encoding.ASCII.GetBytes("TAff0100"); // Treyarch BO2
    private static readonly byte[] S1ff0100Header = Encoding.ASCII.GetBytes("S1ff0100"); // Sledgehammer AW

    // Version bytes for packing (big-endian format)
    private static readonly Dictionary<string, byte[]> VersionBytes = new()
    {
        // COD4
        { "COD4_PS3", new byte[] { 0x00, 0x00, 0x00, 0x01 } },
        { "COD4_PC", new byte[] { 0x00, 0x00, 0x00, 0x05 } },
        { "COD4_Wii", new byte[] { 0x00, 0x00, 0x01, 0xA2 } },
        // WAW/COD5
        { "WAW_PS3", new byte[] { 0x00, 0x00, 0x01, 0x83 } },
        { "WAW_Wii", new byte[] { 0x00, 0x00, 0x01, 0x9B } },
        // MW2 (uses zlib with header instead of raw deflate)
        { "MW2_PS3", new byte[] { 0x00, 0x00, 0x01, 0x0D } },
        { "MW2_PC", new byte[] { 0x00, 0x00, 0x01, 0x14 } },
    };

    // Version detection mapping (version int -> game/platform info)
    private static readonly Dictionary<uint, (string Game, string[] Platforms)> VersionMap = new()
    {
        // COD4
        { 0x01, ("COD4", new[] { "PS3", "Xbox 360" }) },
        { 0x05, ("COD4", new[] { "PC" }) },
        { 0x1A2, ("COD4", new[] { "Wii" }) },
        // WAW
        { 0x183, ("WAW", new[] { "PS3", "Xbox 360", "PC" }) },
        { 0x19B, ("WAW", new[] { "Wii" }) },
        // MW2
        { 0x10D, ("MW2", new[] { "PS3", "Xbox 360" }) },
        { 0x114, ("MW2", new[] { "PC" }) },
        // BO1
        { 0x1D9, ("BO1", new[] { "PS3", "Xbox 360", "PC" }) },
        { 0x1DD, ("BO1", new[] { "Wii" }) },
        // MW3
        { 0x70, ("MW3", new[] { "PS3", "Xbox 360" }) },
        { 0x6B, ("MW3", new[] { "Wii" }) },
        // Note: MW3 PC uses 0x01 which conflicts with COD4
        // BO2
        { 0x92, ("BO2", new[] { "PS3", "Xbox 360" }) },
        { 0x93, ("BO2", new[] { "PC" }) },
        { 0x94, ("BO2", new[] { "Wii U" }) },
        // Quantum of Solace
        { 0x1D6, ("Quantum of Solace", new[] { "PS3", "Xbox 360", "PC" }) },
        { 0x1D2, ("Quantum of Solace", new[] { "Wii" }) },
    };

    public MainForm()
    {
        InitializeComponent();
        PopulateGameVersionComboBox();
    }

    private void PopulateGameVersionComboBox()
    {
        gameVersionComboBox.Items.Clear();
        gameVersionComboBox.Items.AddRange(new object[]
        {
            "COD4 - PS3/Xbox 360 (Unsigned)",
            "COD4 - PC",
            "COD4 - Wii",
            "WAW - PS3/Xbox 360/PC",
            "WAW - Wii",
            "MW2 - PS3/Xbox 360",
            "MW2 - PC"
        });
        gameVersionComboBox.SelectedIndex = 0;
    }

    private (byte[] header, byte[] version) GetPackSettings()
    {
        byte[] header = IWffu100Header; // Default unsigned
        byte[] version;

        switch (gameVersionComboBox.SelectedIndex)
        {
            case 0: version = VersionBytes["COD4_PS3"]; break;
            case 1: version = VersionBytes["COD4_PC"]; break;
            case 2: version = VersionBytes["COD4_Wii"]; break;
            case 3: version = VersionBytes["WAW_PS3"]; break;
            case 4: version = VersionBytes["WAW_Wii"]; break;
            case 5: version = VersionBytes["MW2_PS3"]; break;
            case 6: version = VersionBytes["MW2_PC"]; break;
            default: version = VersionBytes["COD4_PS3"]; break;
        }

        return (header, version);
    }

    private void extractBrowseButton_Click(object sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Title = "Select FastFile to Extract",
            Filter = "FastFiles (*.ff)|*.ff|All Files (*.*)|*.*"
        };

        if (ofd.ShowDialog() == DialogResult.OK)
        {
            extractInputTextBox.Text = ofd.FileName;
            extractOutputTextBox.Text = Path.ChangeExtension(ofd.FileName, ".zone");
            UpdateFileInfo(ofd.FileName);
        }
    }

    private void extractOutputBrowseButton_Click(object sender, EventArgs e)
    {
        using var sfd = new SaveFileDialog
        {
            Title = "Save Zone File As",
            Filter = "Zone Files (*.zone)|*.zone|All Files (*.*)|*.*",
            FileName = Path.GetFileName(extractOutputTextBox.Text)
        };

        if (sfd.ShowDialog() == DialogResult.OK)
        {
            extractOutputTextBox.Text = sfd.FileName;
        }
    }

    private void extractButton_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(extractInputTextBox.Text))
        {
            MessageBox.Show("Please select a FastFile to extract.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!File.Exists(extractInputTextBox.Text))
        {
            MessageBox.Show("Input file not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            extractButton.Enabled = false;
            statusLabel.Text = "Extracting...";
            Application.DoEvents();

            Decompress(extractInputTextBox.Text, extractOutputTextBox.Text);

            var fi = new FileInfo(extractOutputTextBox.Text);
            statusLabel.Text = $"Extracted successfully! ({fi.Length:N0} bytes)";
            MessageBox.Show($"Zone file extracted successfully!\n\nOutput: {extractOutputTextBox.Text}\nSize: {fi.Length:N0} bytes",
                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            statusLabel.Text = "Extraction failed.";
            MessageBox.Show($"Extraction failed:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            extractButton.Enabled = true;
        }
    }

    private void packBrowseButton_Click(object sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Title = "Select Zone File to Pack",
            Filter = "Zone Files (*.zone)|*.zone|All Files (*.*)|*.*"
        };

        if (ofd.ShowDialog() == DialogResult.OK)
        {
            packInputTextBox.Text = ofd.FileName;
            packOutputTextBox.Text = Path.ChangeExtension(ofd.FileName, ".ff");
        }
    }

    private void packOutputBrowseButton_Click(object sender, EventArgs e)
    {
        using var sfd = new SaveFileDialog
        {
            Title = "Save FastFile As",
            Filter = "FastFiles (*.ff)|*.ff|All Files (*.*)|*.*",
            FileName = Path.GetFileName(packOutputTextBox.Text)
        };

        if (sfd.ShowDialog() == DialogResult.OK)
        {
            packOutputTextBox.Text = sfd.FileName;
        }
    }

    private void packButton_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(packInputTextBox.Text))
        {
            MessageBox.Show("Please select a Zone file to pack.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!File.Exists(packInputTextBox.Text))
        {
            MessageBox.Show("Input file not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            packButton.Enabled = false;
            statusLabel.Text = "Packing...";
            Application.DoEvents();

            var (header, version) = GetPackSettings();
            Compress(packInputTextBox.Text, packOutputTextBox.Text, header, version);

            var fi = new FileInfo(packOutputTextBox.Text);
            statusLabel.Text = $"Packed successfully! ({fi.Length:N0} bytes)";
            MessageBox.Show($"FastFile created successfully!\n\nOutput: {packOutputTextBox.Text}\nSize: {fi.Length:N0} bytes",
                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            statusLabel.Text = "Packing failed.";
            MessageBox.Show($"Packing failed:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            packButton.Enabled = true;
        }
    }

    private void UpdateFileInfo(string filePath)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs);

            byte[] header = br.ReadBytes(8);
            byte[] versionBytes = br.ReadBytes(4);

            string headerStr = Encoding.ASCII.GetString(header);

            // Determine header type
            string headerType;
            bool isSigned;
            string studio = "IW";

            if (headerStr == "IWffu100")
            {
                headerType = "IWffu100";
                isSigned = false;
            }
            else if (headerStr == "IWff0100")
            {
                headerType = "IWff0100";
                isSigned = true;
            }
            else if (headerStr == "TAff0100")
            {
                headerType = "TAff0100";
                isSigned = true;
                studio = "Treyarch";
            }
            else if (headerStr == "S1ff0100")
            {
                headerType = "S1ff0100";
                isSigned = true;
                studio = "Sledgehammer";
            }
            else
            {
                fileInfoLabel.Text = $"Unknown header: {headerStr}";
                fileInfoLabel.ForeColor = Color.Red;
                return;
            }

            // Parse version (big-endian)
            uint version = (uint)((versionBytes[0] << 24) | (versionBytes[1] << 16) | (versionBytes[2] << 8) | versionBytes[3]);

            // Detect game and platform
            string game = "Unknown";
            string platforms = "Unknown";

            if (VersionMap.TryGetValue(version, out var info))
            {
                game = info.Game;
                platforms = string.Join("/", info.Platforms);
            }

            // Build info string
            string signedStr = isSigned ? "Signed" : "Unsigned";
            Color signedColor = isSigned ? Color.OrangeRed : Color.DarkGreen;

            fileInfoLabel.Text = $"Header: {headerType} | {signedStr} | Studio: {studio} | Game: {game} | Platform: {platforms} | Version: 0x{version:X}";
            fileInfoLabel.ForeColor = signedColor;

            // Update detailed info
            UpdateDetailedInfo(fs.Length, headerStr, isSigned, studio, game, platforms, version);
        }
        catch (Exception ex)
        {
            fileInfoLabel.Text = $"Error reading file: {ex.Message}";
            fileInfoLabel.ForeColor = Color.Red;
        }
    }

    private void UpdateDetailedInfo(long fileSize, string header, bool isSigned, string studio, string game, string platforms, uint version)
    {
        detailsTextBox.Clear();
        detailsTextBox.AppendText($"File Size: {fileSize:N0} bytes ({fileSize / 1024.0 / 1024.0:F2} MB)\r\n");
        detailsTextBox.AppendText($"Header Magic: {header}\r\n");
        detailsTextBox.AppendText($"Signed: {(isSigned ? "Yes (RSA2048)" : "No")}\r\n");
        detailsTextBox.AppendText($"Studio: {studio}\r\n");
        detailsTextBox.AppendText($"Game: {game}\r\n");
        detailsTextBox.AppendText($"Platform(s): {platforms}\r\n");
        detailsTextBox.AppendText($"Version: 0x{version:X} ({version})\r\n");
        detailsTextBox.AppendText($"\r\n");

        // Check for unsupported games (detection only, extraction/packing won't work)
        bool isUnsupportedGame = game == "BO1" || game == "MW3" || game == "BO2" || game == "Quantum of Solace";

        if (isUnsupportedGame)
        {
            detailsTextBox.AppendText("⚠ Warning: This game is not fully supported.\r\n");
            detailsTextBox.AppendText("Detection only - extraction/packing may not work.\r\n");
            detailsTextBox.AppendText("Supported games: CoD4, WaW, MW2\r\n");
        }
        else if (isSigned)
        {
            detailsTextBox.AppendText("⚠ Warning: This is a signed FastFile.\r\n");
            detailsTextBox.AppendText("Modifications will break the signature.\r\n");
            detailsTextBox.AppendText("Use unsigned versions for modding.\r\n");
        }
        else
        {
            detailsTextBox.AppendText("✓ This FastFile can be modified.\r\n");
        }
    }

    private void Decompress(string inputPath, string outputPath)
    {
        using var br = new BinaryReader(new FileStream(inputPath, FileMode.Open, FileAccess.Read), Encoding.Default);
        using var bw = new BinaryWriter(new FileStream(outputPath, FileMode.Create, FileAccess.Write), Encoding.Default);

        // Read header to determine game version
        byte[] magic = br.ReadBytes(8);
        byte[] versionBytes = br.ReadBytes(4);
        uint version = (uint)((versionBytes[0] << 24) | (versionBytes[1] << 16) | (versionBytes[2] << 8) | versionBytes[3]);

        // MW2 (0x10D, 0x114) has extended header structure
        bool isMW2 = version == 0x10D || version == 0x114;

        if (isMW2)
        {
            // MW2 header: magic(8) + version(4) + allowOnlineUpdate(1) + fileCreationTime(8) +
            //             region(4) + entryCount(4) + entries(entryCount * 0x14) + fileSizes(8)
            byte allowOnlineUpdate = br.ReadByte();           // 1 byte
            byte[] fileCreationTime = br.ReadBytes(8);        // 8 bytes
            byte[] region = br.ReadBytes(4);                  // 4 bytes
            byte[] entryCountBytes = br.ReadBytes(4);         // 4 bytes
            int entryCount = (entryCountBytes[0] << 24) | (entryCountBytes[1] << 16) |
                            (entryCountBytes[2] << 8) | entryCountBytes[3];

            // Skip entries (each entry is 0x14 = 20 bytes on PS3)
            br.ReadBytes(entryCount * 0x14);

            // Skip file sizes (8 bytes)
            br.ReadBytes(8);
        }
        // For CoD4/WaW, we're already at position 12 which is correct

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

        statusLabel.Text = $"Extracted {blockCount} blocks";
    }

    private void Compress(string inputPath, string outputPath, byte[] headerBytes, byte[] versionBytes)
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

        statusLabel.Text = $"Packed {blockCount} blocks";
    }

    private static byte[] DecompressBlock(byte[] compressedData)
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

    private static byte[] CompressBlock(byte[] uncompressedData)
    {
        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal))
        {
            deflate.Write(uncompressedData, 0, uncompressedData.Length);
        }
        return output.ToArray();
    }

    private void MainForm_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            e.Effect = DragDropEffects.Copy;
    }

    private void MainForm_DragDrop(object sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
        {
            string file = files[0];
            string ext = Path.GetExtension(file).ToLower();

            if (ext == ".ff")
            {
                tabControl.SelectedIndex = 0; // Extract tab
                extractInputTextBox.Text = file;
                extractOutputTextBox.Text = Path.ChangeExtension(file, ".zone");
                UpdateFileInfo(file);
            }
            else if (ext == ".zone")
            {
                tabControl.SelectedIndex = 1; // Pack tab
                packInputTextBox.Text = file;
                packOutputTextBox.Text = Path.ChangeExtension(file, ".ff");
            }
        }
    }
}
