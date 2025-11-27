using FastFileCompiler;
using FastFileCompiler.Models;

namespace FastFileCompilerGUI;

public partial class MainForm : Form
{
    private readonly List<RawFileEntry> _rawFiles = new();

    public MainForm()
    {
        InitializeComponent();
        UpdateStatus("Ready - Add files to compile into a FastFile");
    }

    #region File Management

    private void btnAddFiles_Click(object sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select Raw Files to Add",
            Filter = "All Files (*.*)|*.*|GSC Scripts (*.gsc)|*.gsc|Config Files (*.cfg)|*.cfg|String Tables (*.str)|*.str",
            Multiselect = true
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            foreach (var file in dialog.FileNames)
            {
                AddFile(file, Path.GetFileName(file));
            }
            UpdateFileCount();
        }
    }

    private void btnAddFolder_Click(object sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select a folder containing raw files",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            var basePath = dialog.SelectedPath;
            var files = Directory.GetFiles(basePath, "*.*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                // Create relative path as asset name
                var assetName = Path.GetRelativePath(basePath, file).Replace('\\', '/');
                AddFile(file, assetName);
            }
            UpdateFileCount();
        }
    }

    private void AddFile(string sourcePath, string assetName)
    {
        // Check for duplicates
        if (_rawFiles.Any(f => f.AssetName.Equals(assetName, StringComparison.OrdinalIgnoreCase)))
        {
            var result = MessageBox.Show(
                $"Asset '{assetName}' already exists. Replace it?",
                "Duplicate Asset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                RemoveByAssetName(assetName);
            }
            else
            {
                return;
            }
        }

        var entry = new RawFileEntry
        {
            AssetName = assetName,
            SourcePath = sourcePath,
            Size = new FileInfo(sourcePath).Length
        };

        _rawFiles.Add(entry);

        var item = new ListViewItem(new[]
        {
            entry.AssetName,
            FormatSize(entry.Size),
            entry.SourcePath
        })
        {
            Tag = entry
        };

        fileListView.Items.Add(item);
    }

    private void RemoveByAssetName(string assetName)
    {
        var entry = _rawFiles.FirstOrDefault(f => f.AssetName.Equals(assetName, StringComparison.OrdinalIgnoreCase));
        if (entry != null)
        {
            _rawFiles.Remove(entry);
            var item = fileListView.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Tag == entry);
            if (item != null)
                fileListView.Items.Remove(item);
        }
    }

    private void btnRemove_Click(object sender, EventArgs e)
    {
        if (fileListView.SelectedItems.Count == 0) return;

        foreach (ListViewItem item in fileListView.SelectedItems)
        {
            if (item.Tag is RawFileEntry entry)
            {
                _rawFiles.Remove(entry);
            }
            fileListView.Items.Remove(item);
        }
        UpdateFileCount();
    }

    private void btnClear_Click(object sender, EventArgs e)
    {
        if (_rawFiles.Count == 0) return;

        var result = MessageBox.Show(
            "Clear all files from the list?",
            "Confirm Clear",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            _rawFiles.Clear();
            fileListView.Items.Clear();
            UpdateFileCount();
        }
    }

    private void btnMoveUp_Click(object sender, EventArgs e)
    {
        if (fileListView.SelectedItems.Count != 1) return;

        var item = fileListView.SelectedItems[0];
        int index = item.Index;
        if (index > 0)
        {
            fileListView.Items.RemoveAt(index);
            fileListView.Items.Insert(index - 1, item);

            var entry = (RawFileEntry)item.Tag!;
            _rawFiles.Remove(entry);
            _rawFiles.Insert(index - 1, entry);

            item.Selected = true;
            item.Focused = true;
        }
    }

    private void btnMoveDown_Click(object sender, EventArgs e)
    {
        if (fileListView.SelectedItems.Count != 1) return;

        var item = fileListView.SelectedItems[0];
        int index = item.Index;
        if (index < fileListView.Items.Count - 1)
        {
            fileListView.Items.RemoveAt(index);
            fileListView.Items.Insert(index + 1, item);

            var entry = (RawFileEntry)item.Tag!;
            _rawFiles.Remove(entry);
            _rawFiles.Insert(index + 1, entry);

            item.Selected = true;
            item.Focused = true;
        }
    }

    private void btnFixPaths_Click(object sender, EventArgs e)
    {
        if (_rawFiles.Count == 0)
        {
            MessageBox.Show("No files to fix.", "Fix Paths", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Build list of changes
        var changes = new List<(RawFileEntry entry, string oldName, string newName)>();

        foreach (var entry in _rawFiles)
        {
            string newName = FixAssetPath(entry.AssetName);
            if (newName != entry.AssetName)
            {
                changes.Add((entry, entry.AssetName, newName));
            }
        }

        if (changes.Count == 0)
        {
            MessageBox.Show("No paths need fixing. All asset names appear to be correct.", "Fix Paths", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Show preview
        var preview = string.Join("\n", changes.Take(20).Select(c => $"{c.oldName}\n  -> {c.newName}\n"));
        if (changes.Count > 20)
        {
            preview += $"\n... and {changes.Count - 20} more";
        }

        var result = MessageBox.Show(
            $"The following {changes.Count} path(s) will be fixed:\n\n{preview}\n\nApply changes?",
            "Fix Paths - Preview",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes) return;

        // Apply changes
        foreach (var (entry, _, newName) in changes)
        {
            entry.AssetName = newName;
        }

        // Refresh list view
        RefreshFileListView();

        UpdateStatus($"Fixed {changes.Count} asset path(s)");
        MessageBox.Show($"Successfully fixed {changes.Count} asset path(s).", "Fix Paths", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>
    /// Converts flattened asset names back to proper game paths.
    /// Example: maps_mp_gametypes__globallogic.gsc -> maps/mp/gametypes/_globallogic.gsc
    /// </summary>
    private static string FixAssetPath(string assetName)
    {
        // Don't fix if it already contains forward slashes (already a path)
        if (assetName.Contains('/'))
            return assetName;

        // Don't fix simple filenames without known path prefixes
        string[] knownPrefixes = {
            "maps_mp_animscripts_",
            "maps_mp_gametypes_",
            "maps_mp_",
            "clientscripts_mp_",
            "common_scripts_",
            "zzzz_zz_",
            "animscripts_"
        };

        bool hasKnownPrefix = knownPrefixes.Any(p => assetName.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        // If no known prefix, don't change
        if (!hasKnownPrefix)
            return assetName;

        // Get extension
        string extension = Path.GetExtension(assetName);
        string nameOnly = Path.GetFileNameWithoutExtension(assetName);

        // The conversion logic:
        // Original path: clientscripts/mp/_vehicle.csc
        // Flattened:     clientscripts_mp__vehicle.csc
        // Rules:
        //   - Single underscore (_) was a path separator (/)
        //   - Double underscore (__) was path separator + underscore (/_)
        //
        // To reverse:
        // 1. Replace __ with a placeholder that represents /_
        // 2. Replace _ with /
        // 3. Replace placeholder with _

        const string placeholder = "\x01\x02"; // Unique placeholder for underscore in filename

        // Step 1: Replace __ with /<placeholder> (this represents /_)
        string result = nameOnly.Replace("__", "/" + placeholder);

        // Step 2: Replace remaining single _ with /
        result = result.Replace("_", "/");

        // Step 3: Replace placeholder back with underscore
        result = result.Replace(placeholder, "_");

        return result + extension;
    }

    private void RefreshFileListView()
    {
        fileListView.BeginUpdate();
        for (int i = 0; i < fileListView.Items.Count; i++)
        {
            var item = fileListView.Items[i];
            if (item.Tag is RawFileEntry entry)
            {
                item.SubItems[0].Text = entry.AssetName;
            }
        }
        fileListView.EndUpdate();
    }

    private void menuItemRename_Click(object sender, EventArgs e)
    {
        if (fileListView.SelectedItems.Count != 1) return;

        var item = fileListView.SelectedItems[0];
        var entry = (RawFileEntry)item.Tag!;

        using var dialog = new RenameDialog(entry.AssetName);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            entry.AssetName = dialog.NewName;
            item.SubItems[0].Text = entry.AssetName;
        }
    }

    #endregion

    #region Drag and Drop

    private void fileListView_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
        {
            e.Effect = DragDropEffects.Copy;
        }
    }

    private void fileListView_DragDrop(object sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files) return;

        foreach (var path in files)
        {
            if (Directory.Exists(path))
            {
                // It's a folder - add all files
                var basePath = path;
                var innerFiles = Directory.GetFiles(basePath, "*.*", SearchOption.AllDirectories);
                foreach (var file in innerFiles)
                {
                    var assetName = Path.GetRelativePath(basePath, file).Replace('\\', '/');
                    AddFile(file, assetName);
                }
            }
            else if (File.Exists(path))
            {
                AddFile(path, Path.GetFileName(path));
            }
        }
        UpdateFileCount();
    }

    #endregion

    #region Compile

    private async void btnCompile_Click(object sender, EventArgs e)
    {
        if (_rawFiles.Count == 0)
        {
            MessageBox.Show("Please add at least one file to compile.", "No Files", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Save FastFile",
            Filter = "FastFile (*.ff)|*.ff",
            FileName = textBoxZoneName.Text + ".ff"
        };

        if (dialog.ShowDialog() != DialogResult.OK) return;

        var outputPath = dialog.FileName;
        var gameVersion = GetSelectedGameVersion();
        var zoneName = textBoxZoneName.Text;
        var saveZone = checkBoxSaveZone.Checked;

        // Disable UI during compile
        SetUIEnabled(false);
        progressBar.Value = 0;
        UpdateStatus("Compiling...");

        try
        {
            await Task.Run(() =>
            {
                // Build zone
                Invoke(() => UpdateStatus("Building zone file..."));
                Invoke(() => progressBar.Value = 20);

                var builder = new ZoneBuilder(gameVersion, zoneName);

                foreach (var entry in _rawFiles)
                {
                    var rawFile = new RawFile
                    {
                        Name = entry.AssetName,
                        Data = File.ReadAllBytes(entry.SourcePath)
                    };
                    builder.AddRawFile(rawFile);
                }

                Invoke(() => progressBar.Value = 50);
                Invoke(() => UpdateStatus("Compressing..."));

                // Compile
                var compiler = new Compiler(gameVersion);
                var zoneData = builder.Build();

                Invoke(() => progressBar.Value = 70);

                compiler.CompileToFile(zoneData, outputPath, saveZone);

                Invoke(() => progressBar.Value = 100);
            });

            UpdateStatus($"Successfully compiled: {Path.GetFileName(outputPath)}");

            var message = $"FastFile compiled successfully!\n\nOutput: {outputPath}";
            if (saveZone)
            {
                message += $"\nZone: {Path.ChangeExtension(outputPath, ".zone")}";
            }

            MessageBox.Show(message, "Compile Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            UpdateStatus("Compile failed");
            progressBar.Value = 0;
            MessageBox.Show($"Compile failed:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetUIEnabled(true);
        }
    }

    private GameVersion GetSelectedGameVersion()
    {
        return comboBoxGame.SelectedIndex switch
        {
            0 => GameVersion.CoD4,
            1 => GameVersion.WaW,
            2 => GameVersion.MW2,
            _ => GameVersion.CoD4
        };
    }

    #endregion

    #region UI Helpers

    private void UpdateStatus(string status)
    {
        labelStatus.Text = status;
    }

    private void UpdateFileCount()
    {
        long totalSize = _rawFiles.Sum(f => f.Size);
        UpdateStatus($"{_rawFiles.Count} file(s), {FormatSize(totalSize)} total");
    }

    private void SetUIEnabled(bool enabled)
    {
        btnAddFiles.Enabled = enabled;
        btnAddFolder.Enabled = enabled;
        btnRemove.Enabled = enabled;
        btnClear.Enabled = enabled;
        btnMoveUp.Enabled = enabled;
        btnMoveDown.Enabled = enabled;
        btnCompile.Enabled = enabled;
        comboBoxGame.Enabled = enabled;
        textBoxZoneName.Enabled = enabled;
        checkBoxSaveZone.Enabled = enabled;
        fileListView.Enabled = enabled;
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }

    #endregion

    #region Menu Events

    private void exitMenuItem_Click(object sender, EventArgs e)
    {
        Close();
    }

    private void aboutMenuItem_Click(object sender, EventArgs e)
    {
        MessageBox.Show(
            "FastFile Compiler GUI\n\n" +
            "A tool for creating PS3 Call of Duty FastFiles (.ff)\n" +
            "from raw game files.\n\n" +
            "Supports:\n" +
            "- Call of Duty 4: Modern Warfare\n" +
            "- Call of Duty: World at War\n" +
            "- Call of Duty: Modern Warfare 2",
            "About",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    #endregion
}

/// <summary>
/// Represents a raw file entry in the list.
/// </summary>
public class RawFileEntry
{
    public string AssetName { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public long Size { get; set; }
}
