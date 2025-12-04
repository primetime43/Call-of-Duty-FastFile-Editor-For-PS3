using Call_of_Duty_FastFile_Editor.CodeOperations;
using Call_of_Duty_FastFile_Editor.Constants;
using Call_of_Duty_FastFile_Editor.IO;
using Call_of_Duty_FastFile_Editor.Models;
using Call_of_Duty_FastFile_Editor.Services;
using Call_of_Duty_FastFile_Editor.UI;
using Call_of_Duty_FastFile_Editor.ZoneParsers;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using static Call_of_Duty_FastFile_Editor.Service.GitHubReleaseChecker;

namespace Call_of_Duty_FastFile_Editor
{
    public partial class MainWindowForm : Form
    {
        private TreeNode _previousSelectedNode;
        private readonly IRawFileService _rawFileService;

        /// <summary>
        /// List of raw file nodes extracted from the zone file.
        /// </summary>
        private List<RawFileNode> _rawFileNodes;

        private List<LocalizedEntry> _localizedEntries;

        /// <summary>
        /// List of menu lists extracted from the zone file.
        /// </summary>
        private List<MenuList> _menuLists;

        /// <summary>
        /// List of tags extracted from the zone file.
        /// </summary>
        private TagCollection? _tags;

        /// <summary>
        /// Offset where the assset pool starts in the zone file.
        /// </summary>
        private int _assetPoolStartOffset;

        /// <summary>
        /// Offset where the assset pool ends in the zone file.
        /// </summary>
        private int _assetPoolEndOffset;

        /// <summary>
        /// FastFile instance representing the opened Fast File.
        /// </summary>
        private FastFile _openedFastFile;

        /// <summary>
        /// List of ZoneAssetRecords extracted from the opened Fast File's zone.
        /// </summary>
        private List<ZoneAssetRecord> _zoneAssetRecords;

        private AssetRecordCollection _processResult;
        private IFastFileHandler _fastFileHandler;

        public MainWindowForm(IRawFileService rawFileService)
        {
            InitializeComponent();
            _rawFileService = rawFileService;
            textEditorControlEx1.SyntaxHighlighting = "C#";
            this.SetProgramTitle();

            // Universal toolstrip menu item
            copyToolStripMenuItem.Click += copyToolStripMenuItem_Click;
            universalContextMenu.Opening += universalContextMenu_Opening;
            this.FormClosing += MainWindowForm_FormClosing;
        }

        #region Right Click Context Menu initialization
        private string _rightClickedItemText = string.Empty;

        private void universalContextMenu_Opening(object sender, CancelEventArgs e)
        {
            copyToolStripMenuItem.Enabled = !string.IsNullOrEmpty(_rightClickedItemText);
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_rightClickedItemText))
            {
                Clipboard.SetText(_rightClickedItemText);
            }
        }
        #endregion

        /// <summary>
        /// Parses and processes the asset records from the opened Fast File's zone.
        /// </summary>
        private void LoadAssetRecordsData(bool forcePatternMatching = false, bool loadRawFiles = true, bool loadLocalizedEntries = true, bool loadTags = true)
        {
            var zone = _openedFastFile.OpenedFastFileZone;

            // Set the zone asset records to this form's field
            _zoneAssetRecords = zone.ZoneFileAssets.ZoneAssetRecords;

            // Set these so it's shorter/easier to use them later
            _assetPoolStartOffset = zone.AssetPoolStartOffset;
            _assetPoolEndOffset = zone.AssetPoolEndOffset;

            // Process asset records - uses structure-based parsing first, then pattern matching fallback
            _processResult = AssetRecordProcessor.ProcessAssetRecords(_openedFastFile, _zoneAssetRecords, forcePatternMatching);

            // store the typed lists based on user selection
            _rawFileNodes = loadRawFiles ? _processResult.RawFileNodes : new List<RawFileNode>();
            RawFileNode.CurrentZone = zone;
            _localizedEntries = loadLocalizedEntries ? _processResult.LocalizedEntries : new List<LocalizedEntry>();
            _menuLists = _processResult.MenuLists ?? new List<MenuList>();

            // also store updated records
            _zoneAssetRecords = _processResult.UpdatedRecords;

            // Parse and populate tags if selected
            if (loadTags)
            {
                PopulateTags();
                // Ensure tags tab is visible
                if (!mainTabControl.TabPages.Contains(tagsTabPage))
                {
                    mainTabControl.TabPages.Insert(2, tagsTabPage); // Insert after Asset Pool and Raw Files
                }
            }
            else
            {
                _tags = null;
                // Hide tags tab
                if (mainTabControl.TabPages.Contains(tagsTabPage))
                {
                    mainTabControl.TabPages.Remove(tagsTabPage);
                }
            }
        }

        /// <summary>
        /// Loads all parsed zone data into the UI components for display.
        /// </summary>
        private void LoadZoneDataToUI()
        {
            // Load the asset pool into the ListView
            // The data LoadAssetRecordsData gets
            LoadAssetPoolIntoListView();

            // Load the raw files into the TreeView
            LoadRawFilesTreeView();

            // Load the values parsed from the zone header (tag count, asset record count)
            LoadZoneHeaderValues(_openedFastFile.OpenedFastFileZone);

            PopulateLocalizeAssets();
            PopulateMenuFiles();
            PopulateCollision_Map_Asset_StringData();
        }

        /// <summary>
        /// Once all data has been loaded to the UI, show UI elements that were previously hidden/disabled.
        /// </summary>
        private void EnableUI_Elements()
        {
            // Enable relevant menu items
            renameRawFileToolStripMenuItem.Enabled = true;
            saveFastFileToolStripMenuItem.Enabled = true;
            saveFastFileAsToolStripMenuItem.Enabled = true;
        }

        /// <summary>
        /// Populates the TreeView with TreeNodes corresponding to RawFileNodes.
        /// </summary>
        private void LoadRawFilesTreeView()
        {
            // Clear existing nodes to avoid duplication
            filesTreeView.Nodes.Clear();

            var treeNodes = _rawFileNodes.Select(node =>
            {
                var treeNode = new TreeNode(node.FileName)
                {
                    Tag = node // Associate TreeNode with RawFileNode via Tag
                };
                return treeNode;
            }).ToArray();

            filesTreeView.Nodes.AddRange(treeNodes);
            UIManager.SetRawFileTreeNodeColors(filesTreeView);
        }

        /// <summary>
        /// Handles actions before selecting a new TreeView node, prompting to save unsaved changes.
        /// </summary>
        private void filesTreeView_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            if (_previousSelectedNode?.Tag is RawFileNode prevNode && prevNode.HasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes to this file. Do you want to save before switching?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    // Save the previous node
                    _rawFileService.SaveZoneRawFileChanges(
                        filesTreeView,
                        _openedFastFile.FfFilePath,
                        _openedFastFile.ZoneFilePath,
                        _rawFileNodes,
                        prevNode.RawFileContent,
                        _openedFastFile
                    );
                    prevNode.HasUnsavedChanges = false;
                }
                else if (result == DialogResult.Cancel)
                {
                    // Cancel the switch entirely
                    e.Cancel = true;
                    return;
                }
                else // DialogResult.No → discard changes
                {
                    // Revert the node’s content back to the last‐loaded bytes
                    var originalText = Encoding.UTF8.GetString(prevNode.RawFileBytes);
                    prevNode.RawFileContent = originalText;
                    prevNode.HasUnsavedChanges = false;

                    // Immediately update the editor so the user sees the discard
                    textEditorControlEx1.TextChanged -= textEditorControlEx1_TextChanged;
                    textEditorControlEx1.SetTextAndRefresh(originalText);
                    textEditorControlEx1.TextChanged += textEditorControlEx1_TextChanged;
                }
            }

            // Now allow the selection to change
            _previousSelectedNode = filesTreeView.SelectedNode;
        }

        /// <summary>
        /// Handles actions after selecting a new TreeView node, loading the corresponding file content.
        /// </summary>
        private void filesTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is RawFileNode selectedNode)
            {
                // Load the file content into the editor without retriggering TextChanged
                textEditorControlEx1.TextChanged -= textEditorControlEx1_TextChanged;
                textEditorControlEx1.SetTextAndRefresh(selectedNode.RawFileContent ?? string.Empty);
                textEditorControlEx1.TextChanged += textEditorControlEx1_TextChanged;

                // Update UI
                UIManager.UpdateSelectedFileStatusStrip(selectedItemStatusLabel, selectedNode.FileName);
                UIManager.UpdateStatusStrip(
                    selectedFileMaxSizeStatusLabel,
                    selectedFileCurrentSizeStatusLabel,
                    selectedNode.MaxSize,
                    textEditorControlEx1.Text.Length
                );

                // Reset this node’s dirty flag now that its content is in sync
                selectedNode.HasUnsavedChanges = false;

                // Track for BeforeSelect logic
                _previousSelectedNode = e.Node;
            }
        }

        /// <summary>
        /// Handles text changes in the editor, marking the content as unsaved.
        /// </summary>
        private void textEditorControlEx1_TextChanged(object sender, EventArgs e)
        {
            // Fetch the selected node from the TreeView
            if (filesTreeView.SelectedNode?.Tag is RawFileNode selectedNode)
            {
                // Update the RawFileContent in memory
                selectedNode.RawFileContent = textEditorControlEx1.Text;
                // Mark the file as having unsaved changes (dirty)
                selectedNode.HasUnsavedChanges = true;

                // The "current size" is simply the length of the editor text
                int currentSize = textEditorControlEx1.Text.Length;

                // Update the status strip to reflect the new size
                UIManager.UpdateStatusStrip(
                    selectedFileMaxSizeStatusLabel,       // The label displaying "Max Size: XYZ"
                    selectedFileCurrentSizeStatusLabel,   // The label displaying "Current Size: XYZ"
                    selectedNode.MaxSize,                 // The raw file's maximum allowed size
                    currentSize                           // The new size in the editor
                );
            }
        }

        /// <summary>
        /// Saves the current Fast File, recompressing it.
        /// Automatically applies any pending editor changes first.
        /// </summary>
        private void saveFastFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_openedFastFile == null || _fastFileHandler == null)
            {
                MessageBox.Show("No Fast File is currently opened.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Sync current editor text to the selected node
                var selectedNode = GetSelectedRawFileNode();
                if (selectedNode != null)
                {
                    selectedNode.RawFileContent = textEditorControlEx1.Text;
                }

                // Sync RawFileContent to RawFileBytes for ALL nodes with changes
                foreach (var node in _rawFileNodes)
                {
                    if (node.HasUnsavedChanges && !string.IsNullOrEmpty(node.RawFileContent))
                    {
                        node.RawFileBytes = Encoding.ASCII.GetBytes(node.RawFileContent);
                    }
                }

                // Rebuild the zone file from parsed assets (only includes supported types)
                string zoneName = Path.GetFileNameWithoutExtension(_openedFastFile.FfFilePath);
                byte[]? newZoneData = ZoneFileBuilder.BuildFreshZone(_rawFileNodes, _localizedEntries, _openedFastFile, zoneName);

                if (newZoneData == null)
                {
                    MessageBox.Show("Failed to rebuild zone file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Write the new zone data
                File.WriteAllBytes(_openedFastFile.ZoneFilePath, newZoneData);

                // Recompress to FF
                _fastFileHandler?.Recompress(_openedFastFile.FfFilePath, _openedFastFile.ZoneFilePath, _openedFastFile);
                MessageBox.Show($"Fast File saved to:\n\n{_openedFastFile.FfFilePath}\n\n" +
                                $"Zone rebuilt with {_rawFileNodes.Count} rawfiles and {_localizedEntries?.Count ?? 0} localized entries.",
                                "Saved",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Asterisk);

                // Reset every node's dirty flag now that the zone is saved
                foreach (var node in _rawFileNodes)
                    node.HasUnsavedChanges = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save Fast File: {ex.Message}",
                                "Save Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Saves the Fast File as a new file.
        /// Automatically applies any pending editor changes first.
        /// </summary>
        private void saveFastFileAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_openedFastFile == null || _fastFileHandler == null)
            {
                MessageBox.Show("No Fast File is currently opened.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Fast Files (*.ff)|*.ff|All Files (*.*)|*.*";
                saveFileDialog.Title = "Save Fast File As";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Sync current editor text to the selected node
                        var selectedNode = GetSelectedRawFileNode();
                        if (selectedNode != null)
                        {
                            selectedNode.RawFileContent = textEditorControlEx1.Text;
                        }

                        // Sync RawFileContent to RawFileBytes for ALL nodes with changes
                        foreach (var node in _rawFileNodes)
                        {
                            if (node.HasUnsavedChanges && !string.IsNullOrEmpty(node.RawFileContent))
                            {
                                node.RawFileBytes = Encoding.ASCII.GetBytes(node.RawFileContent);
                            }
                        }

                        string newFilePath = saveFileDialog.FileName;

                        // Rebuild the zone file from parsed assets (only includes supported types)
                        string zoneName = Path.GetFileNameWithoutExtension(newFilePath);
                        byte[]? newZoneData = ZoneFileBuilder.BuildFreshZone(_rawFileNodes, _localizedEntries, _openedFastFile, zoneName);

                        if (newZoneData == null)
                        {
                            MessageBox.Show("Failed to rebuild zone file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        // Write the new zone data to a temp file for compression
                        string tempZonePath = Path.Combine(Path.GetTempPath(), zoneName + ".zone");
                        File.WriteAllBytes(tempZonePath, newZoneData);

                        // Recompress to new FF path
                        _fastFileHandler?.Recompress(newFilePath, tempZonePath, _openedFastFile);

                        // Clean up temp file
                        if (File.Exists(tempZonePath))
                            File.Delete(tempZonePath);

                        MessageBox.Show($"Fast File saved to:\n\n{newFilePath}\n\n" +
                                        $"Zone rebuilt with {_rawFileNodes.Count} rawfiles and {_localizedEntries?.Count ?? 0} localized entries.",
                                        "Saved",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Asterisk);

                        // Clear all per-node dirty flags
                        foreach (var node in _rawFileNodes)
                            node.HasUnsavedChanges = false;

                        // Then close out
                        SaveCloseFastFileAndCleanUp();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to save Fast File As: {ex.Message}",
                                        "Save Error",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Exits the application.
        /// </summary>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveCloseFastFileAndCleanUp(!keepZoneFileToolStripMenuItem.Checked);
            Close();
        }

        /// <summary>
        /// Removes comments from the code in the editor.
        /// </summary>
        private void removeCommentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                textEditorControlEx1.Text = CommentRemover.RemoveCStyleComments(textEditorControlEx1.Text);
                textEditorControlEx1.Text = CommentRemover.RemoveCustomComments(textEditorControlEx1.Text);
                textEditorControlEx1.Text = Regex.Replace(textEditorControlEx1.Text, "(\\r\\n){2,}", "\r\n\r\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to remove comments: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Compresses the code in the editor.
        /// </summary>
        private void compressCodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                textEditorControlEx1.Text = CommentRemover.RemoveCStyleComments(textEditorControlEx1.Text);
                textEditorControlEx1.Text = CommentRemover.RemoveCustomComments(textEditorControlEx1.Text);
                textEditorControlEx1.Text = CodeCompressor.CompressCode(textEditorControlEx1.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to compress code: {ex.Message}", "Compression Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Checks the syntax of the code in the editor.
        /// </summary>
        private void checkSyntaxToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                SyntaxChecker.CheckSyntax(textEditorControlEx1.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Syntax check failed: {ex.Message}", "Syntax Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void checkForUpdate()
        {
            try
            {
                var release = await ReleaseChecker.CheckForNewRelease("primetime43", "CoD-FF-Tools");

                if (release != null)
                {
                    int latestReleaseInt = ReleaseChecker.convertVersionToInt(release.tag_name);
                    int localProgramVersionInt = ReleaseChecker.convertVersionToInt(ApplicationConstants.ProgramVersion);

                    if (latestReleaseInt > localProgramVersionInt)
                    {
                        DialogResult result = MessageBox.Show(
                            "Current version: " + ApplicationConstants.ProgramVersion + "\nNew release available: " + release.name + " (" + release.tag_name + ")\nDo you want to download it?",
                            "New Release",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question
                        );

                        if (result == DialogResult.Yes)
                        {
                            try
                            {
                                var startInfo = new ProcessStartInfo
                                {
                                    FileName = ReleaseChecker.releaseURL,
                                    UseShellExecute = true
                                };

                                Process.Start(startInfo);
                            }
                            catch (System.ComponentModel.Win32Exception ex)
                            {
                                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                    else if (latestReleaseInt == localProgramVersionInt)
                    {
                        MessageBox.Show("You are using the latest version.", "No Update Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        Debug.WriteLine("Local version is newer than the latest release.");
                    }
                }
                else
                {
                    MessageBox.Show("No new releases available.", "Update Check", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to check for updates: {ex.Message}", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void injectFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Inject a File";
                ofd.Filter = "Allowed Files (*.cfg;*.gsc;*.atr;*.csc;*.rmb;*.arena;*.vision)|*.cfg;*.gsc;*.atr;*.csc;*.rmb;*.arena;*.vision|All Files (*.*)|*.*";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string selectedFilePath = ofd.FileName;
                    string rawFileName = Path.GetFileName(selectedFilePath);

                    // Detect if file has zone header or is a plain file
                    bool hasZoneHeader = DetectZoneHeader(selectedFilePath);

                    string rawFileNameFromHeader;
                    byte[] rawFileContent;
                    int newFileMaxSize;

                    if (hasZoneHeader)
                    {
                        // Parse the file to obtain expected header details.
                        var parsedNodes = RawFileParser.ExtractAllRawFilesSizeAndName(selectedFilePath);
                        if (parsedNodes == null || parsedNodes.Count == 0)
                        {
                            MessageBox.Show("Failed to parse the file header. The file may be corrupted.",
                                "Parse Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        RawFileNode newRawFileNode = parsedNodes[0];
                        rawFileNameFromHeader = newRawFileNode.FileName;
                        rawFileContent = newRawFileNode.RawFileBytes;
                        newFileMaxSize = newRawFileNode.MaxSize;
                    }
                    else
                    {
                        // Plain file - prompt user for game path
                        string gamePath = PromptForGamePath(rawFileName);
                        if (string.IsNullOrWhiteSpace(gamePath))
                        {
                            MessageBox.Show("Injection canceled. A game path is required for plain files.",
                                "Injection Canceled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        rawFileNameFromHeader = gamePath;
                        rawFileContent = File.ReadAllBytes(selectedFilePath);
                        newFileMaxSize = rawFileContent.Length;
                    }

                    // Check if a file with the same header name already exists.
                    RawFileNode existingNode = _rawFileNodes
                        .FirstOrDefault(node => node.FileName.Equals(rawFileNameFromHeader, StringComparison.OrdinalIgnoreCase));

                    if (existingNode != null)
                    {
                        try
                        {
                            if (newFileMaxSize > existingNode.MaxSize)
                                _rawFileService.IncreaseSize(_openedFastFile.ZoneFilePath, existingNode, rawFileContent);
                            else
                                _rawFileService.UpdateFileContent(_openedFastFile.ZoneFilePath, existingNode, rawFileContent);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to update file: {ex.Message}",
                                "Injection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                    else
                    {
                        // File doesn't exist in the FastFile - cannot add new raw files here
                        MessageBox.Show(
                            $"The raw file '{rawFileNameFromHeader}' does not exist in this FastFile.\n\n" +
                            "This tool can only update existing raw files.\n\n" +
                            "To add custom raw files, use the FF Compiler to build a custom FastFile.",
                            "File Not Found",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }

                    RefreshZoneData();
                    ReloadAllRawFileNodesAndUI();
                    MessageBox.Show($"File '{rawFileName}' was successfully updated in the zone file.",
                        "Update Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        /// <summary>
        /// Detects if a file has a zone header (FF FF FF FF markers).
        /// </summary>
        private bool DetectZoneHeader(string filePath)
        {
            try
            {
                byte[] header = new byte[12];
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    if (fs.Length < 12)
                        return false;
                    fs.Read(header, 0, 12);
                }

                // Check for first marker (FF FF FF FF) at offset 0
                bool hasFirstMarker = header[0] == 0xFF && header[1] == 0xFF &&
                                       header[2] == 0xFF && header[3] == 0xFF;

                // Check for second marker (FF FF FF FF) at offset 8
                bool hasSecondMarker = header[8] == 0xFF && header[9] == 0xFF &&
                                        header[10] == 0xFF && header[11] == 0xFF;

                return hasFirstMarker && hasSecondMarker;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Prompts the user to enter a game path for a plain file.
        /// Automatically converts filename conventions like "maps__load.gsc" to "maps/_load.gsc"
        /// where "__" represents "/" in the game path.
        /// </summary>
        private string PromptForGamePath(string defaultFileName)
        {
            // Convert filename to game path by replacing "__" with "/"
            // e.g., "maps__load.gsc" -> "maps/_load.gsc"
            // e.g., "maps_mp_gametypes__dm.gsc" -> "maps/mp/gametypes/_dm.gsc"
            string suggestedPath = ConvertFileNameToGamePath(defaultFileName);

            using (var dialog = new RenameDialog(suggestedPath))
            {
                dialog.Text = "Enter Game Path";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    return dialog.NewFileName;
                }
                return null;
            }
        }

        /// <summary>
        /// Converts a filename with underscore conventions to a game path.
        /// - Single underscore followed by another underscore ("__") becomes "/_" (underscore file/folder name)
        /// - Single underscore at word boundary becomes "/" (path separator)
        /// Examples:
        /// - "maps__load.gsc" -> "maps/_load.gsc"
        /// - "maps_mp_gametypes_dm.gsc" -> "maps/mp/gametypes/dm.gsc"
        /// </summary>
        private string ConvertFileNameToGamePath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return fileName;

            // Replace "__" with a placeholder first (for underscore-prefixed names like "_load")
            const string placeholder = "\x00UNDERSCORE\x00";
            string result = fileName.Replace("__", placeholder);

            // Replace remaining single underscores with forward slashes
            result = result.Replace("_", "/");

            // Restore the underscore-prefixed names
            result = result.Replace(placeholder, "/_");

            return result;
        }

        /// <summary>
        /// Exports the selected file, including its header information, to a chosen location.
        /// This format can be re-injected back into a FastFile.
        /// </summary>
        private void exportFileMenuItem_Click(object sender, EventArgs e)
        {
            RawFileNode selectedNode = GetSelectedRawFileNode();
            if (selectedNode == null)
                return;

            string fileExtension = Path.GetExtension(selectedNode.FileName);
            _rawFileService.ExportRawFile(selectedNode, fileExtension);
        }

        /// <summary>
        /// Exports only the content of the selected file without zone header.
        /// This is a clean script file for external editing.
        /// </summary>
        private void exportContentOnlyMenuItem_Click(object sender, EventArgs e)
        {
            RawFileNode selectedNode = GetSelectedRawFileNode();
            if (selectedNode == null)
                return;

            string fileExtension = Path.GetExtension(selectedNode.FileName);
            _rawFileService.ExportRawFileContentOnly(selectedNode, fileExtension);
        }

        /// <summary>
        /// Close the opened fast file, clear the tree view, and reset the form.
        /// Recompresses the zone file back into the fast file. (saves changes)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeFastFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveCloseFastFileAndCleanUp(!keepZoneFileToolStripMenuItem.Checked);
        }

        private void renameFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PerformRawFileRename();
        }

        /// <summary>
        /// Handles the Edit menu Rename Raw File click.
        /// </summary>
        private void renameRawFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PerformRawFileRename();
        }

        /// <summary>
        /// Performs the raw file rename operation.
        /// Used by both the Edit menu and context menu rename options.
        /// </summary>
        private void PerformRawFileRename()
        {
            RawFileNode selectedNode = GetSelectedRawFileNode();
            if (selectedNode == null)
                return;

            _rawFileService.RenameRawFile(filesTreeView, _openedFastFile.FfFilePath, _openedFastFile.ZoneFilePath, _rawFileNodes, _openedFastFile);
            ReloadAllRawFileNodesAndUI();
        }

        /// <summary>
        /// Populates the DataGridView with Zone decimal values.
        /// </summary>
        private void LoadZoneHeaderValues(ZoneFile zone)
        {
            if (zone == null || zone.HeaderFieldValues == null)
            {
                _openedFastFile.OpenedFastFileZone.ReadHeaderFields();
            }

            // Convert the dictionary to a list of objects with matching property names
            var dataSource = zone.HeaderFieldValues.Select(kvp => new
            {
                ZoneName = kvp.Key,
                ZoneDecValue = kvp.Value,
                ZoneHexValue = Utilities.ConvertToBigEndianHex(kvp.Value),
                ZoneOffset = _openedFastFile.OpenedFastFileZone.GetZoneOffset(kvp.Key)
            }).ToList();

            // Assign the data source to the DataGridView
            zoneInfoDataGridView.DataSource = dataSource;
        }

        /// <summary>
        /// Populates the Tags page view with the tags extracted from the zone file.
        /// </summary>
        private void PopulateTags()
        {
            // Fetch the results
            _tags = TagOperations.FindTags(_openedFastFile.OpenedFastFileZone);

            if (_tags == null)
                return;

            tagsListView.View = View.Details;
            tagsListView.Columns.Clear();
            tagsListView.Items.Clear();
            tagsListView.MultiSelect = true;
            tagsListView.FullRowSelect = true;
            tagsListView.Columns.Add("Tag (" + _tags.TagEntries.Count + ")", 100);
            tagsListView.Columns.Add("Offset", 100);

            // Sort the TagEntries by OffsetDec in ascending order
            var sortedTagEntries = _tags.TagEntries
                .OrderBy(entry => entry.OffsetDec)
                .ToList();

            // Now tagsInfo.TagEntries holds all entries
            foreach (var entry in _tags.TagEntries)
            {
                // 1) Tag
                // 2) Hex offset
                var lvi = new ListViewItem(entry.Tag);

                // Hex offset (for example "0x1AC4AC0")
                lvi.SubItems.Add("0x" + entry.OffsetHex);

                tagsListView.Items.Add(lvi);
            }

            tagsListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void PopulateLocalizeAssets()
        {
            // Check if we have any localized entries in our processed results.
            if (_localizedEntries == null || _localizedEntries.Count <= 0)
            {
                mainTabControl.TabPages.Remove(localizeTabPage); // hide the tab page if there's no data to show
                return;
            }

            // Clear any existing items and columns.
            localizeListView.Items.Clear();
            localizeListView.Columns.Clear();

            // Set up the ListView.
            localizeListView.View = View.Details;
            localizeListView.FullRowSelect = true;
            localizeListView.GridLines = true;

            // Add the required columns with "Text" as the last column.
            localizeListView.Columns.Add("Key", 120);
            localizeListView.Columns.Add("Start Offset", 100);
            localizeListView.Columns.Add("End Offset", 100);
            localizeListView.Columns.Add("Size", 80);
            localizeListView.Columns.Add("Text", 300);

            // Loop through each localized entry.
            foreach (var entry in _localizedEntries)
            {
                // Calculate the size difference.
                int size = entry.EndOfFileHeader - entry.StartOfFileHeader;

                // Create a new ListViewItem with the Key as the main text.
                ListViewItem lvi = new ListViewItem(entry.Key);

                // Add subitems in the new order.
                lvi.SubItems.Add($"0x{entry.StartOfFileHeader:X}");
                lvi.SubItems.Add($"0x{entry.EndOfFileHeader:X}");
                lvi.SubItems.Add($"0x{size:X}");
                lvi.SubItems.Add(entry.LocalizedText);

                // Add the ListViewItem to the ListView.
                localizeListView.Items.Add(lvi);
            }

            // Auto-resize columns to fit header size.
            localizeListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void PopulateMenuFiles()
        {
            // Menu files in CoD are actually rawfiles with .menu extension
            // Filter rawfiles to show only .menu files (like Red-EyeX32 does)
            var menuRawFiles = _rawFileNodes?
                .Where(r => r.FileName != null &&
                       (r.FileName.EndsWith(".menu", StringComparison.OrdinalIgnoreCase) ||
                        r.FileName.Contains("menu", StringComparison.OrdinalIgnoreCase) && r.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)))
                .ToList() ?? new List<RawFileNode>();

            if (menuRawFiles.Count <= 0)
            {
                mainTabControl.TabPages.Remove(menuFilesTabPage); // hide the tab page if there's no data to show
                return;
            }

            // Ensure the tab is shown
            if (!mainTabControl.TabPages.Contains(menuFilesTabPage))
            {
                mainTabControl.TabPages.Add(menuFilesTabPage);
            }

            // Clear any existing items and columns.
            menuFilesListView.Items.Clear();
            menuFilesListView.Columns.Clear();

            // Set up the ListView.
            menuFilesListView.View = View.Details;
            menuFilesListView.FullRowSelect = true;
            menuFilesListView.GridLines = true;

            // Add the required columns.
            menuFilesListView.Columns.Add("File Name", 300);
            menuFilesListView.Columns.Add("Size", 100);
            menuFilesListView.Columns.Add("Start Offset", 100);
            menuFilesListView.Columns.Add("End Offset", 100);

            // Loop through each menu rawfile.
            foreach (var rawFile in menuRawFiles)
            {
                // Create a new ListViewItem with the FileName as the main text.
                ListViewItem lvi = new ListViewItem(rawFile.FileName);

                // Add subitems.
                lvi.SubItems.Add($"{rawFile.MaxSize} bytes");
                lvi.SubItems.Add($"0x{rawFile.StartOfFileHeader:X}");
                lvi.SubItems.Add($"0x{rawFile.RawFileEndPosition:X}");

                // Store the rawfile reference for potential selection handling
                lvi.Tag = rawFile;

                // Add the ListViewItem to the ListView.
                menuFilesListView.Items.Add(lvi);
            }

            // Auto-resize columns to fit header size.
            menuFilesListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void PopulateCollision_Map_Asset_StringData()
        {
            int offset = Collision_Map_Operations.FindCollision_Map_DataOffsetViaFF(_openedFastFile.OpenedFastFileZone);
            if (offset == -1)
            {
                //MessageBox.Show("No map header found near large FF blocks.");
                mainTabControl.TabPages.Remove(collision_Map_AssetTabPage); // hide the tab page if there's no data to show
                return;
            }

            // Parse entities from that offset
            List<MapEntity> mapTest = Collision_Map_Operations.ParseMapEntsAtOffset(_openedFastFile.OpenedFastFileZone, offset);

            if (mapTest.Count == 0)
            {
                MessageBox.Show("Found an offset, but parsing yielded no entities.", "Empty",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Sort by "classname"
            mapTest.Sort((entA, entB) =>
            {
                entA.Properties.TryGetValue("classname", out string aClass);
                entB.Properties.TryGetValue("classname", out string bClass);
                aClass ??= "";
                bClass ??= "";
                return string.Compare(aClass, bClass, StringComparison.OrdinalIgnoreCase);
            });

            // Clear the TreeView
            treeViewMapEnt.Nodes.Clear();

            // Attempt to get both the map size and its offset
            var sizeInfo = Collision_Map_Operations.GetMapDataSizeAndOffset(_openedFastFile.OpenedFastFileZone, mapTest);
            if (sizeInfo.HasValue)
            {
                // Destructure the tuple
                (int mapSize, int offsetOfSize) = sizeInfo.Value;

                // Create a top-level node for the map size
                TreeNode sizeNode = new TreeNode($"Map Data Size = {mapSize} bytes (dec)");
                treeViewMapEnt.Nodes.Add(sizeNode);

                // Create another node for where that size is stored (in hex)
                string sizeOffsetHex = offsetOfSize.ToString("X");
                TreeNode sizeOffsetNode = new TreeNode($"Map Size AssetPoolRecordOffset = 0x{sizeOffsetHex}");
                treeViewMapEnt.Nodes.Add(sizeOffsetNode);
            }
            else
            {
                // If we can't find the size, optionally show a node or just skip
                treeViewMapEnt.Nodes.Add("Could not detect map size.");
            }

            // Now add each entity
            for (int i = 0; i < mapTest.Count; i++)
            {
                MapEntity entity = mapTest[i];

                // Parent label from "classname" or fallback
                string parentLabel = entity.Properties.TryGetValue("classname", out string classNameVal)
                    ? classNameVal
                    : $"Entity {i}";

                TreeNode parentNode = new TreeNode(parentLabel);

                // Show offset in hex
                string offsetHex = entity.SourceOffset.ToString("X");
                parentNode.Nodes.Add($"AssetPoolRecordOffset = 0x{offsetHex}");

                // Then the key-value pairs
                foreach (var kvp in entity.Properties)
                {
                    if (!kvp.Key.Equals("classname", StringComparison.OrdinalIgnoreCase))
                    {
                        parentNode.Nodes.Add($"{kvp.Key} = {kvp.Value}");
                    }
                }

                treeViewMapEnt.Nodes.Add(parentNode);
            }
        }

        private void listView_MouseDownCopy(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var lv = (ListView)sender;
                ListViewHitTestInfo hit = lv.HitTest(e.Location);
                if (hit.Item != null)
                {
                    _rightClickedItemText = hit.SubItem.Text;
                }
                else
                {
                    _rightClickedItemText = string.Empty;
                }
            }
        }

        private void treeView_MouseDownCopy(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var tv = (TreeView)sender;
                TreeNode node = tv.GetNodeAt(e.X, e.Y);
                if (node != null)
                {
                    tv.SelectedNode = node;
                    _rightClickedItemText = node.Text;
                }
                else
                {
                    _rightClickedItemText = string.Empty;
                }
            }
        }

        private void dataGrid_MouseDownCopy(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var dgv = (DataGridView)sender;
                // Get row/column of the clicked cell
                DataGridView.HitTestInfo hit = dgv.HitTest(e.X, e.Y);

                if (hit.RowIndex >= 0 && hit.ColumnIndex >= 0)
                {
                    // Optionally select the clicked row/cell
                    dgv.ClearSelection();
                    dgv.Rows[hit.RowIndex].Selected = true;
                    dgv.CurrentCell = dgv[hit.ColumnIndex, hit.RowIndex];

                    // Store the cell's value in our right-clicked text
                    object cellValue = dgv[hit.ColumnIndex, hit.RowIndex].Value;
                    _rightClickedItemText = cellValue?.ToString() ?? string.Empty;
                }
                else
                {
                    // Right-clicked outside a valid cell
                    _rightClickedItemText = string.Empty;
                }
            }
        }

        private void SaveCloseFastFileAndCleanUp(bool deleteZoneFile = false)
        {
            try
            {
                if (_openedFastFile != null && File.Exists(_openedFastFile.FfFilePath))
                {
                    // Store the path before we null it out
                    string savedPath = _openedFastFile.FfFilePath;

                    // Always save before closing
                    _fastFileHandler?.Recompress(_openedFastFile.FfFilePath, _openedFastFile.ZoneFilePath, _openedFastFile);

                    // We no longer have a form‑level dirty flag to clear
                    ResetAllViews();

                    if (deleteZoneFile)
                    {
                        try { File.Delete(_openedFastFile.ZoneFilePath); }
                        catch { }
                    }

                    _openedFastFile = null;
                    MessageBox.Show($"Fast File Saved & Closed.\n\nSaved to:\n{savedPath}", "Close Complete",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to close fastfile: {ex.Message}",
                                "Close Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Opens a Form to search for text throughout all of the raw files
        /// </summary>
        private void searchRawFileTxtMenuItem_Click(object sender, EventArgs e)
        {
            if (_rawFileNodes?.Count > 0)
                new RawFileSearcherForm(_rawFileNodes).Show();
            else
                MessageBox.Show("No raw files found to search through.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Populates the Zone Asset Pool list view from the _zoneAssetRecords.
        /// Shows ALL assets from the asset pool, with parsed data where available.
        /// </summary>
        private void LoadAssetPoolIntoListView()
        {
            // Make sure we have valid data
            if (_openedFastFile == null ||
                _openedFastFile.OpenedFastFileZone == null ||
                _zoneAssetRecords == null)
            {
                return;
            }

            // Clear existing items and columns
            assetPoolListView.Items.Clear();
            assetPoolListView.Columns.Clear();

            // Use "Details" view with full-row select
            assetPoolListView.View = View.Details;
            assetPoolListView.FullRowSelect = true;
            assetPoolListView.GridLines = true;

            // Get game definition for asset type names
            var gameDefinition = GameDefinitions.GameDefinitionFactory.GetDefinition(_openedFastFile);

            // Count total parsed assets
            int totalParsed = (_rawFileNodes?.Count ?? 0) + (_localizedEntries?.Count ?? 0) + (_menuLists?.Count ?? 0);
            int totalAssets = _zoneAssetRecords.Count;

            // Columns for the list view
            assetPoolListView.Columns.Add($"Index", 60);
            assetPoolListView.Columns.Add("Asset Type", 120);
            assetPoolListView.Columns.Add("Pool Offset", 90);
            assetPoolListView.Columns.Add("Data Start", 90);
            assetPoolListView.Columns.Add("Data End", 90);
            assetPoolListView.Columns.Add("Size", 70);
            assetPoolListView.Columns.Add("Name", 250);
            assetPoolListView.Columns.Add("Status", 200);

            // Place the asset pool header info at the top
            var pool = new ListViewItem("--");
            pool.SubItems.Add("ASSET POOL");
            pool.SubItems.Add($"0x{_assetPoolStartOffset:X}");
            pool.SubItems.Add($"0x{_assetPoolEndOffset:X}");
            pool.SubItems.Add("");
            pool.SubItems.Add($"0x{(_assetPoolEndOffset - _assetPoolStartOffset):X}");
            pool.SubItems.Add($"Total: {totalAssets} assets, {totalParsed} parsed");
            pool.SubItems.Add("");
            pool.Font = new System.Drawing.Font(assetPoolListView.Font, System.Drawing.FontStyle.Bold);
            assetPoolListView.Items.Add(pool);

            // Create lookup sets for quick matching against parsed data
            int rawFileIndex = 0;
            int localizeIndex = 0;
            int menuIndex = 0;

            // Iterate through ALL asset records from the asset pool
            for (int i = 0; i < _zoneAssetRecords.Count; i++)
            {
                var record = _zoneAssetRecords[i];

                // Get the asset type based on game
                int assetTypeValue = GetAssetTypeValue(record);
                string assetTypeName = gameDefinition.GetAssetTypeName(assetTypeValue);
                bool isRawFile = gameDefinition.IsRawFileType(assetTypeValue);
                bool isLocalize = gameDefinition.IsLocalizeType(assetTypeValue);
                bool isMenuFile = gameDefinition.IsMenuFileType(assetTypeValue);

                var lvi = new ListViewItem((i + 1).ToString());
                lvi.SubItems.Add(assetTypeName);
                lvi.SubItems.Add($"0x{record.AssetPoolRecordOffset:X}");

                // Check if this record has been parsed by matching against parsed lists
                bool isParsed = false;
                string name = "-";
                string dataStart = "-";
                string dataEnd = "-";
                string size = "-";
                string status = "Not parsed (unsupported type)";

                if (isRawFile && _rawFileNodes != null && rawFileIndex < _rawFileNodes.Count)
                {
                    var node = _rawFileNodes[rawFileIndex];
                    isParsed = true;
                    name = node.FileName ?? "-";
                    dataStart = $"0x{node.CodeStartPosition:X}";
                    dataEnd = $"0x{node.CodeEndPosition:X}";
                    size = $"0x{node.MaxSize:X}";
                    status = node.AdditionalData ?? "Parsed";
                    rawFileIndex++;
                }
                else if (isLocalize && _localizedEntries != null && localizeIndex < _localizedEntries.Count)
                {
                    var entry = _localizedEntries[localizeIndex];
                    isParsed = true;
                    name = entry.Key ?? "-";
                    dataStart = $"0x{entry.StartOfFileHeader:X}";
                    dataEnd = $"0x{entry.EndOfFileHeader:X}";
                    int entrySize = entry.EndOfFileHeader - entry.StartOfFileHeader;
                    size = $"0x{entrySize:X}";
                    status = "Localized asset parsed";
                    localizeIndex++;
                }
                else if (isMenuFile && _menuLists != null && menuIndex < _menuLists.Count)
                {
                    var menu = _menuLists[menuIndex];
                    isParsed = true;
                    name = menu.Name ?? "-";
                    dataStart = $"0x{menu.DataStartOffset:X}";
                    dataEnd = $"0x{menu.DataEndOffset:X}";
                    int menuSize = menu.DataEndOffset - menu.DataStartOffset;
                    size = $"0x{menuSize:X}";
                    status = $"MenuList parsed ({menu.MenuCount} menus)";
                    menuIndex++;
                }

                lvi.SubItems.Add(dataStart);
                lvi.SubItems.Add(dataEnd);
                lvi.SubItems.Add(size);
                lvi.SubItems.Add(name);
                lvi.SubItems.Add(status);

                if (!isParsed)
                {
                    lvi.ForeColor = System.Drawing.Color.Gray;
                }

                assetPoolListView.Items.Add(lvi);
            }

            // Auto-resize columns to fit header size or content
            assetPoolListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        /// <summary>
        /// Gets the asset type value from a record based on the current game.
        /// </summary>
        private int GetAssetTypeValue(Models.ZoneAssetRecord record)
        {
            if (_openedFastFile.IsCod4File)
                return (int)record.AssetType_COD4;
            if (_openedFastFile.IsCod5File)
                return (int)record.AssetType_COD5;
            if (_openedFastFile.IsMW2File)
                return (int)record.AssetType_MW2;
            return 0;
        }

        /// <summary>
        /// Extracts all raw file content (without zone headers) to a chosen folder.
        /// </summary>
        private void extractAllRawFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Check that there are raw files available
            if (_rawFileNodes == null || _rawFileNodes.Count == 0)
            {
                MessageBox.Show("No raw files available for extraction.", "No Files", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Prompt the user to choose a destination folder
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Select the destination folder to extract all raw files";
                if (folderBrowserDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                string destinationFolder = folderBrowserDialog.SelectedPath;

                // Loop through all raw file nodes and write each one to the selected folder
                foreach (var rawFileNode in _rawFileNodes)
                {
                    try
                    {
                        // Replace any forward or backslashes in the file name with underscores to avoid invalid characters
                        string safeFileName = rawFileNode.FileName.Replace("/", "_").Replace("\\", "_");

                        // Construct the destination file path using the sanitized file name
                        string destFilePath = Path.Combine(destinationFolder, safeFileName);

                        // Write the raw content bytes to the destination file (no zone headers)
                        File.WriteAllBytes(destFilePath, rawFileNode.RawFileBytes);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to extract {rawFileNode.FileName}: {ex.Message}",
                            "Extraction Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }

                MessageBox.Show("All raw files extracted successfully.",
                    "Extraction Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        // move this eventually maybe
        private void RefreshZoneData()
        {
            if (_openedFastFile == null)
                return;

            // 1) Fully re-read the zone file bytes from disk
            _openedFastFile.OpenedFastFileZone.LoadData();

            // 2) Re-run your asset record processing logic
            //    This updates _rawFileNodes, _zoneAssetRecords, _stringTables, etc.
            LoadAssetRecordsData();

            // 3) Rebuild the entire UI
            //    (Clears the TreeView/ListViews and reloads all data)
            ResetAllViews();
            LoadZoneDataToUI();
        }

        /// <summary>
        /// Clears out the relevant UI elements, so they can be repopulated cleanly.
        /// </summary>
        private void ResetAllViews()
        {
            filesTreeView.Nodes.Clear();
            assetPoolListView.Items.Clear();
            assetPoolListView.Columns.Clear();
            tagsListView.Items.Clear();
            tagsListView.Columns.Clear();
            localizeListView.Items.Clear();
            localizeListView.Columns.Clear();
            treeViewMapEnt.Nodes.Clear();
            foreach (var lv in new[] { tagsListView, assetPoolListView, localizeListView })
            {
                lv.Items.Clear();
                lv.Columns.Clear();
            }
            zoneInfoDataGridView.DataSource = null;
            textEditorControlEx1.ResetText();
            loadedFileNameStatusLabel.Visible = false;
            selectedFileMaxSizeStatusLabel.Visible = false;
            selectedItemStatusLabel.Visible = false;
            selectedFileCurrentSizeStatusLabel.Visible = false;
            saveFastFileToolStripMenuItem.Enabled = false;
            saveFastFileAsToolStripMenuItem.Enabled = false;
            this.SetProgramTitle();
        }


        private void MainWindowForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && _rawFileNodes != null)
            {
                // Find any raw files with unsaved changes
                var dirtyNodes = _rawFileNodes.Where(n => n.HasUnsavedChanges).ToList();
                if (dirtyNodes.Count > 0)
                {
                    var result = MessageBox.Show(
                        $"You have unsaved changes in {dirtyNodes.Count} file(s). Save before exiting?",
                        "Unsaved Changes",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        // Save each dirty file
                        foreach (var node in dirtyNodes)
                        {
                            // Select the corresponding TreeNode so SaveZoneRawFileChanges targets it
                            var treeNode = filesTreeView.Nodes
                                .OfType<TreeNode>()
                                .First(t => ReferenceEquals(t.Tag, node));
                            filesTreeView.SelectedNode = treeNode;

                            _rawFileService.SaveZoneRawFileChanges(
                                filesTreeView,
                                _openedFastFile.FfFilePath,
                                _openedFastFile.ZoneFilePath,
                                _rawFileNodes,
                                node.RawFileContent,
                                _openedFastFile
                            );
                            node.HasUnsavedChanges = false;
                        }
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        // Cancel the close
                        e.Cancel = true;
                        return;
                    }
                    // if No, proceed and discard unsaved changes
                }

                // Clean up the temp zone file (unless user wants to keep it)
                if (!keepZoneFileToolStripMenuItem.Checked &&
                    _openedFastFile != null && File.Exists(_openedFastFile.ZoneFilePath))
                {
                    try
                    {
                        File.Delete(_openedFastFile.ZoneFilePath);
                    }
                    catch
                    {
                        // ignore any deletion errors
                    }
                }
            }
        }

        /// <summary>
        /// Adjust the size of the selected raw file node.
        /// Rebuilds the entire zone file to avoid data corruption from byte shifting.
        /// </summary>
        private void increaseFileSizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RawFileNode selectedNode = GetSelectedRawFileNode();
            if (selectedNode == null)
                return;

            // Create the size adjust dialog and pass in the current file size.
            using (RawFileSizeAdjust sizeAdjustDialog = new RawFileSizeAdjust())
            {
                sizeAdjustDialog.CurrentFileSize = selectedNode.MaxSize;
                if (sizeAdjustDialog.ShowDialog(this) == DialogResult.OK)
                {
                    int newSize = sizeAdjustDialog.NewFileSize;
                    if (newSize <= selectedNode.MaxSize)
                    {
                        MessageBox.Show("The new size must be greater than the current size.",
                            "Invalid Size", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    try
                    {
                        // Update the selected node's max size and pad its content
                        int oldSize = selectedNode.MaxSize;
                        byte[] currentContent = selectedNode.RawFileBytes ?? Array.Empty<byte>();
                        byte[] newContent = new byte[newSize];
                        Array.Copy(currentContent, newContent, Math.Min(currentContent.Length, newSize));

                        selectedNode.MaxSize = newSize;
                        selectedNode.RawFileBytes = newContent;
                        selectedNode.RawFileContent = System.Text.Encoding.Default.GetString(newContent);

                        // Rebuild the zone file from all raw file nodes
                        byte[]? newZoneData = ZoneFileBuilder.BuildFreshZone(
                            _rawFileNodes,
                            _localizedEntries,
                            _openedFastFile,
                            Path.GetFileNameWithoutExtension(_openedFastFile.FastFileName));

                        if (newZoneData == null)
                        {
                            // Revert the changes
                            selectedNode.MaxSize = oldSize;
                            selectedNode.RawFileBytes = currentContent;
                            selectedNode.RawFileContent = System.Text.Encoding.Default.GetString(currentContent);

                            MessageBox.Show("Failed to rebuild zone file.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        // Write the new zone data
                        File.WriteAllBytes(_openedFastFile.ZoneFilePath, newZoneData);

                        MessageBox.Show($"File '{selectedNode.FileName}' size increased to {newSize} bytes successfully.\nZone file rebuilt.",
                            "Size Increase Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        RefreshZoneData();
                        ReloadAllRawFileNodesAndUI();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error increasing file size: {ex.Message}",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the currently selected RawFileNode from the TreeView.
        /// If no node is selected or the selected node does not have a valid RawFileNode,
        /// a message box is shown and the method returns null.
        /// </summary>
        private RawFileNode GetSelectedRawFileNode()
        {
            if (filesTreeView.SelectedNode == null || !(filesTreeView.SelectedNode.Tag is RawFileNode selectedNode))
            {
                MessageBox.Show("Please select a raw file.", "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
            return selectedNode;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(ApplicationConstants.About, "About Call of Duty Fast File Editor");
        }

        private void CheckForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            checkForUpdate();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            if (_openedFastFile == null)
            {
                MessageBox.Show("Open a .ff first", "No Zone Loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // make sure bytes are loaded
            var zoneData = _openedFastFile.OpenedFastFileZone.Data;
            var hexForm = new ZoneHexViewForm(zoneData);
            hexForm.Show();
        }

        /// <summary>
        /// Creates a backup of the FastFile if one doesn't already exist.
        /// </summary>
        private void CreateBackupIfNeeded(string filePath)
        {
            try
            {
                string backupPath = filePath + ".bak";
                if (!File.Exists(backupPath))
                {
                    File.Copy(filePath, backupPath);
                    Debug.WriteLine($"[Backup] Created backup: {backupPath}");
                }
                else
                {
                    Debug.WriteLine($"[Backup] Backup already exists: {backupPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Backup] Failed to create backup: {ex.Message}");
                // Don't block opening the file if backup fails
            }
        }

        private void COD5ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_openedFastFile != null)
            {
                SaveCloseFastFileAndCleanUp();
            }

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select a COD5 Fast File",
                Filter = "Fast Files (*.ff)|*.ff"
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            // Create a backup of the original FastFile before any modifications
            CreateBackupIfNeeded(openFileDialog.FileName);

            try
            {
                _openedFastFile = new FastFile(openFileDialog.FileName);
                UIManager.UpdateLoadedFileNameStatusStrip(loadedFileNameStatusLabel, _openedFastFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to read FastFile header: {ex.Message}", "Header Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_openedFastFile.IsValid)
            {
                try
                {
                    // Assign the correct handler for the opened file
                    _fastFileHandler = FastFileHandlerFactory.GetHandler(_openedFastFile);

                    // Show the opened FF path in the program's title text
                    this.SetProgramTitle(_openedFastFile.FfFilePath);

                    // Decompress the Fast File to get the zone file
                    _fastFileHandler.Decompress(_openedFastFile.FfFilePath, _openedFastFile.ZoneFilePath);

                    // Load & parse that zone in one go
                    _openedFastFile.LoadZone();

                    // Get tag count for the dialog
                    int tagCount = TagOperations.GetTagCount(_openedFastFile.OpenedFastFileZone);

                    // Show asset selection dialog
                    bool loadRawFiles = true;
                    bool loadLocalizedEntries = true;
                    bool loadTags = true;

                    using (var assetDialog = new AssetSelectionDialog(
                        _openedFastFile.OpenedFastFileZone.ZoneFileAssets.ZoneAssetRecords,
                        _openedFastFile,
                        tagCount))
                    {
                        if (assetDialog.ShowDialog(this) == DialogResult.Cancel)
                        {
                            SaveCloseFastFileAndCleanUp();
                            return;
                        }
                        loadRawFiles = assetDialog.LoadRawFiles;
                        loadLocalizedEntries = assetDialog.LoadLocalizedEntries;
                        loadTags = assetDialog.LoadTags;
                    }

                    // Here is where the asset records actual data is parsed throughout the zone
                    LoadAssetRecordsData(loadRawFiles: loadRawFiles, loadLocalizedEntries: loadLocalizedEntries, loadTags: loadTags);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to parse zone: {ex.Message}", "Zone Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    // Load all the parsed data from the zone file to the UI
                    LoadZoneDataToUI();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Loading data failed: {ex.Message}", "Data Loading Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else
            {
                MessageBox.Show("Invalid FastFile!\n\nThe FastFile you have selected is not a valid PS3 .ff!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }
            EnableUI_Elements();
        }

        private void cOD4ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_openedFastFile != null)
            {
                SaveCloseFastFileAndCleanUp();
            }

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select a COD4 Fast File",
                Filter = "Fast Files (*.ff)|*.ff"
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            // Create a backup of the original FastFile before any modifications
            CreateBackupIfNeeded(openFileDialog.FileName);

            try
            {
                _openedFastFile = new FastFile(openFileDialog.FileName);
                UIManager.UpdateLoadedFileNameStatusStrip(loadedFileNameStatusLabel, _openedFastFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to read FastFile header: {ex.Message}", "Header Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_openedFastFile.IsValid)
            {
                try
                {
                    // Assign the correct handler for the opened file
                    _fastFileHandler = FastFileHandlerFactory.GetHandler(_openedFastFile);

                    // Show the opened FF path in the program's title text
                    this.SetProgramTitle(_openedFastFile.FfFilePath);

                    // Decompress the Fast File to get the zone file
                    _fastFileHandler.Decompress(_openedFastFile.FfFilePath, _openedFastFile.ZoneFilePath);

                    // Load & parse that zone in one go
                    _openedFastFile.LoadZone();

                    // Get tag count for the dialog
                    int tagCount = TagOperations.GetTagCount(_openedFastFile.OpenedFastFileZone);

                    // Show asset selection dialog
                    bool loadRawFiles = true;
                    bool loadLocalizedEntries = true;
                    bool loadTags = true;

                    using (var assetDialog = new AssetSelectionDialog(
                        _openedFastFile.OpenedFastFileZone.ZoneFileAssets.ZoneAssetRecords,
                        _openedFastFile,
                        tagCount))
                    {
                        if (assetDialog.ShowDialog(this) == DialogResult.Cancel)
                        {
                            SaveCloseFastFileAndCleanUp();
                            return;
                        }
                        loadRawFiles = assetDialog.LoadRawFiles;
                        loadLocalizedEntries = assetDialog.LoadLocalizedEntries;
                        loadTags = assetDialog.LoadTags;
                    }

                    // Here is where the asset records actual data is parsed throughout the zone
                    LoadAssetRecordsData(loadRawFiles: loadRawFiles, loadLocalizedEntries: loadLocalizedEntries, loadTags: loadTags);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to parse zone: {ex.Message}", "Zone Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    // Load all the parsed data from the zone file to the UI
                    LoadZoneDataToUI();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Loading data failed: {ex.Message}", "Data Loading Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else
            {
                MessageBox.Show("Invalid FastFile!\n\nThe FastFile you have selected is not a valid PS3 .ff!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }
            EnableUI_Elements();
        }

        private void mW2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_openedFastFile != null)
            {
                SaveCloseFastFileAndCleanUp();
            }

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select a MW2 Fast File",
                Filter = "Fast Files (*.ff)|*.ff"
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            // Create a backup of the original FastFile before any modifications
            CreateBackupIfNeeded(openFileDialog.FileName);

            try
            {
                _openedFastFile = new FastFile(openFileDialog.FileName);
                UIManager.UpdateLoadedFileNameStatusStrip(loadedFileNameStatusLabel, _openedFastFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to read FastFile header: {ex.Message}", "Header Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_openedFastFile.IsValid)
            {
                try
                {
                    // Assign the correct handler for the opened file
                    _fastFileHandler = FastFileHandlerFactory.GetHandler(_openedFastFile);

                    // Show the opened FF path in the program's title text
                    this.SetProgramTitle(_openedFastFile.FfFilePath);

                    // Decompress the Fast File to get the zone file
                    _fastFileHandler.Decompress(_openedFastFile.FfFilePath, _openedFastFile.ZoneFilePath);

                    // Load & parse that zone in one go
                    _openedFastFile.LoadZone();

                    // Get tag count for the dialog
                    int tagCount = TagOperations.GetTagCount(_openedFastFile.OpenedFastFileZone);

                    // Show asset selection dialog
                    bool loadRawFiles = true;
                    bool loadLocalizedEntries = true;
                    bool loadTags = true;

                    using (var assetDialog = new AssetSelectionDialog(
                        _openedFastFile.OpenedFastFileZone.ZoneFileAssets.ZoneAssetRecords,
                        _openedFastFile,
                        tagCount))
                    {
                        if (assetDialog.ShowDialog(this) == DialogResult.Cancel)
                        {
                            SaveCloseFastFileAndCleanUp();
                            return;
                        }
                        loadRawFiles = assetDialog.LoadRawFiles;
                        loadLocalizedEntries = assetDialog.LoadLocalizedEntries;
                        loadTags = assetDialog.LoadTags;
                    }

                    // Here is where the asset records actual data is parsed throughout the zone
                    LoadAssetRecordsData(loadRawFiles: loadRawFiles, loadLocalizedEntries: loadLocalizedEntries, loadTags: loadTags);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to parse zone: {ex.Message}", "Zone Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    // Load all the parsed data from the zone file to the UI
                    LoadZoneDataToUI();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Loading data failed: {ex.Message}", "Data Loading Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else
            {
                MessageBox.Show("Invalid FastFile!\n\nThe FastFile you have selected is not a valid PS3 MW2 .ff!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }
            EnableUI_Elements();
        }

        // There's a lot of duplicate code around this issue. This needs revisited & fixed/cleaned up
        private void ReloadAllRawFileNodesAndUI()
        {
            // Reparse the raw file nodes from disk
            _rawFileNodes = RawFileParser.ExtractAllRawFilesSizeAndName(_openedFastFile.ZoneFilePath);
            RawFileNode.CurrentZone = _openedFastFile.OpenedFastFileZone;

            // Rebuild UI for files list
            LoadRawFilesTreeView();
        }

        private void reloadRawFilesPatternMatchingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadAssetRecordsData(forcePatternMatching: true);
            ResetAllViews();
            LoadZoneDataToUI();
        }

        private void reportIssuesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string url = "https://github.com/primetime43/CoD-FF-Tools/issues";
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
