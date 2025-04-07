using Call_of_Duty_FastFile_Editor.CodeOperations;
using Call_of_Duty_FastFile_Editor.IO;
using Call_of_Duty_FastFile_Editor.Models;
using Call_of_Duty_FastFile_Editor.UI;
using System.Text.RegularExpressions;
using Call_of_Duty_FastFile_Editor.Original_Fast_Files;
using System.Diagnostics;
using static Call_of_Duty_FastFile_Editor.Service.GitHubReleaseChecker;
using System.Text;
using Call_of_Duty_FastFile_Editor.FileOperations;
using Call_of_Duty_FastFile_Editor.Services;
using System.ComponentModel;
using Call_of_Duty_FastFile_Editor.ZoneParsers;
using ICSharpCode.TextEditorEx;

namespace Call_of_Duty_FastFile_Editor
{
    public partial class MainWindowForm : Form
    {
        private string _programVersion = "v1.0.0";
        private string _originalFastFilesPath = Path.Combine(Application.StartupPath, "Original Fast Files");
        private TreeNode _previousSelectedNode;
        private bool _hasUnsavedChanges = false;


        /// <summary>
        /// List of raw file nodes extracted from the zone file.
        /// </summary>
        private List<RawFileNode> _rawFileNodes;

        /// <summary>
        /// List of string tables extracted from the zone file.
        /// </summary>
        private List<StringTable> _stringTables;

        private List<LocalizedEntry> _localizedEntries;

        /// <summary>
        /// List of tags extracted from the zone file.
        /// </summary>
        private Tags? _tags;

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

        private ZoneAssetRecords _processResult;

        public MainWindowForm()
        {
            InitializeComponent();
            textEditorControlEx1.SyntaxHighlighting = "C#";

            DirectoryInfo directoryInfo = new DirectoryInfo(_originalFastFilesPath);
            directoryInfo.Attributes |= FileAttributes.Hidden;
            this.Text = $"Call of Duty Fast File Editor for PS3 - {_programVersion}";

            // Universal toolstrip menu item
            copyToolStripMenuItem.Click += copyToolStripMenuItem_Click;
            universalContextMenu.Opening += universalContextMenu_Opening;
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
        /// Opens a Fast File, decompresses it, extracts data from the zone & displays it.
        /// </summary>
        private void openFastFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_openedFastFile != null)
            {
                CloseFastFileAndCleanUp();
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

            try
            {
                _openedFastFile = new FastFile(openFileDialog.FileName);
                UIManager.UpdateLoadedFileNameStatusStrip(loadedFileNameStatusLabel, _openedFastFile.FastFileName);
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
                    // Decompress the Fast File to get the zone file
                    FastFileProcessing.DecompressFastFile(_openedFastFile.FfFilePath, _openedFastFile.ZoneFilePath);

                    // Read the byte of the zone file and set them to the Zone object
                    _openedFastFile.OpenedFastFileZone.GetSetZoneBytes();

                    // Find the asset pool, parse it, and set it to the Zone object
                    _openedFastFile.OpenedFastFileZone.GetSetZoneAssetPool();

                    // Here is where the asset records actual data is parsed throughout the zone
                    LoadAssetRecordsData();
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

        /// <summary>
        /// Parses and processes the asset records from the opened Fast File's zone.
        /// </summary>
        private void LoadAssetRecordsData()
        {
            // Set the zone asset records to this form's field
            _zoneAssetRecords = _openedFastFile.OpenedFastFileZone.ZoneFileAssets.ZoneAssetRecords;

            // Set these so it's shorter/easier to use them later
            _assetPoolStartOffset = _openedFastFile.OpenedFastFileZone.AssetPoolStartOffset;
            _assetPoolEndOffset = _openedFastFile.OpenedFastFileZone.AssetPoolEndOffset;

            // Anything that needs to be displayed for the asset pool view tab should be loaded here

            _processResult = AssetRecordProcessor.ProcessAssetRecords(_openedFastFile, _zoneAssetRecords);

            // store the typed lists
            _rawFileNodes = _processResult.RawFileNodes;
            RawFileNode.CurrentZone = _openedFastFile.OpenedFastFileZone;
            _stringTables = _processResult.StringTables;
            _localizedEntries = _processResult.LocalizedEntries;

            // also store updated records
            _zoneAssetRecords = _processResult.UpdatedRecords;


            // REWRITE EVENTUALLY. At this point we should know the location of the asset pool start
            // So we can go back one from the start and there be a null byte, then the tags end starts there
            PopulateTags();
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

            PopulateStringTable();
            PopulateLocalizeAssets();
            PopulateCollision_Map_Asset_StringData();
        }

        /// <summary>
        /// Once all data has been loaded to the UI, show UI elements that were previously hidden/disabled.
        /// </summary>
        private void EnableUI_Elements()
        {
            // Enable relevant menu items
            saveRawFileToolStripMenuItem.Enabled = true;
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
            if (_hasUnsavedChanges)
            {
                DialogResult result = MessageBox.Show(
                    "You have unsaved changes. Do you want to save before switching?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (result == DialogResult.Yes)
                {
                    if (_previousSelectedNode != null && _previousSelectedNode.Tag is RawFileNode previousNode)
                    {
                        RawFileOperations.Save(
                            filesTreeView,                // TreeView control
                            _openedFastFile.FfFilePath,     // Path to the Fast File (.ff)
                            _openedFastFile.ZoneFilePath,   // Path to the decompressed zone file
                            _rawFileNodes,                  // List of RawFileNode objects
                            textEditorControlEx1.Text,        // Updated text from the editor
                            _openedFastFile                // FastFile instance
                        );
                    }
                    _hasUnsavedChanges = false;
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true; // Cancel the selection change
                    return;
                }
            }
            _previousSelectedNode = filesTreeView.SelectedNode; // Save the current node for later use
        }

        /// <summary>
        /// Handles actions after selecting a new TreeView node, loading the corresponding file content.
        /// </summary>
        private void filesTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag is RawFileNode selectedNode)
            {
                string fileName = selectedNode.FileName;
                int maxSize = selectedNode.MaxSize;
                string fileContent = selectedNode.RawFileContent ?? string.Empty;

                // Update the editor content without triggering multiple events.
                textEditorControlEx1.TextChanged -= textEditorControlEx1_TextChanged;
                //textEditorControlEx1.Text = fileContent;
                textEditorControlEx1.SetTextAndRefresh(selectedNode.RawFileContent);
                textEditorControlEx1.TextChanged += textEditorControlEx1_TextChanged;

                // Update UI elements.
                UIManager.UpdateSelectedFileStatusStrip(selectedItemStatusLabel, fileName);
                UIManager.UpdateStatusStrip(
                    selectedFileMaxSizeStatusLabel,
                    selectedFileCurrentSizeStatusLabel,
                    maxSize,
                    textEditorControlEx1.Text.Length
                );
                _hasUnsavedChanges = false; // Reset unsaved flag after loading content
            }
        }

        /// <summary>
        /// Handles text changes in the editor, marking the content as unsaved.
        /// </summary>
        private void textEditorControlEx1_TextChanged(object sender, EventArgs e)
        {
            if (filesTreeView.SelectedNode?.Tag is int position)
            {
                var selectedNode = _rawFileNodes.FirstOrDefault(node => node.PatternIndexPosition == position);
                if (selectedNode != null)
                {
                    int maxSize = selectedNode.MaxSize;
                    UIManager.UpdateStatusStrip(
                        selectedFileMaxSizeStatusLabel,
                        selectedFileCurrentSizeStatusLabel,
                        maxSize,
                        textEditorControlEx1.Text.Length
                    );
                    _hasUnsavedChanges = true;
                }
            }
        }

        /// <summary>
        /// Saves the current Fast File, recompressing it.
        /// </summary>
        private void saveFastFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                FastFileProcessing.RecompressFastFile(_openedFastFile.FfFilePath, _openedFastFile.ZoneFilePath, _openedFastFile);
                MessageBox.Show("Fast File saved to:\n\n" + _openedFastFile.FfFilePath, "Saved", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                _hasUnsavedChanges = false; // Reset the flag after saving
                CloseFastFileAndCleanUp();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save Fast File: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Saves the Fast File as a new file.
        /// </summary>
        private void saveFastFileAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Fast Files (*.ff)|*.ff|All Files (*.*)|*.*";
                saveFileDialog.Title = "Save Fast File As";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string newFilePath = saveFileDialog.FileName;
                        FastFileProcessing.RecompressFastFile(_openedFastFile.FfFilePath, newFilePath, _openedFastFile);
                        MessageBox.Show("Fast File saved to:\n\n" + newFilePath, "Saved", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                        _hasUnsavedChanges = false; // Reset the flag after saving
                        CloseFastFileAndCleanUp();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to save Fast File As: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Exits the application.
        /// </summary>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseFastFileAndCleanUp(true);
            Close();
        }

        /// <summary>
        /// Saves the raw file using SaveRawFile utility.
        /// </summary>
        private void saveRawFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                RawFileOperations.Save(
                    filesTreeView,                // TreeView control
                    _openedFastFile.FfFilePath,                   // Path to the Fast File (.ff)
                    _openedFastFile.ZoneFilePath,                 // Path to the decompressed zone file
                    _rawFileNodes,               // List of RawFileNode objects
                    textEditorControlEx1.Text,      // Updated text from the editor
                    _openedFastFile                       // FastFile instance
                );
                _hasUnsavedChanges = false; // Reset the flag after saving
                MessageBox.Show("Raw file saved successfully.", "Save Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save raw file: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
        /// Saves the selected file to PC.
        /// </summary>
        private void saveFileToPCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (filesTreeView.SelectedNode == null)
            {
                MessageBox.Show("Please select a file to save.", "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!(filesTreeView.SelectedNode.Tag is int position))
            {
                MessageBox.Show("Selected node does not have a valid position.", "Invalid Selection", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var selectedFileNode = _rawFileNodes.FirstOrDefault(node => node.PatternIndexPosition == position);
            if (selectedFileNode == null)
            {
                MessageBox.Show("Selected file node not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Title = "Choose where to save Asset...";
                saveFileDialog.FileName = Path.GetFileName(selectedFileNode.FileName);
                saveFileDialog.Filter = "All Files (*.*)|*.*";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(selectedFileNode.RawFileContent))
                        {
                            MessageBox.Show("Selected text file has no content to save.", "Empty File", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        // Determine encoding based on your requirements
                        Encoding encoding = Encoding.UTF8; // or another appropriate encoding
                        File.WriteAllText(saveFileDialog.FileName, selectedFileNode.RawFileContent, encoding);

                        MessageBox.Show($"File successfully saved to:\n\n{saveFileDialog.FileName}", "Save Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to save file: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
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

        /// <summary>
        /// Displays the About dialog.
        /// </summary>
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string message = "Call of Duty Fast File Editor for PS3\n" +
                             "Version: " + _programVersion + "\n\n" +
                             "Developed by primetime43\n\n" +
                             "Supported Games\n" +
                             "- COD4 (Modern Warfare)\n" +
                             "- COD5 (World at War)\n\n" +
                             "Special thanks to:\n" +
                             "- BuC-ShoTz\n" +
                             "- aerosoul94\n" +
                             "- EliteMossy\n\n" +
                             "GitHub: https://github.com/primetime43";
            MessageBox.Show(message, "About Call of Duty Fast File Editor");
        }

        /// <summary>
        /// Downloads the default Fast File for COD5.
        /// </summary>
        private void defaultffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DownloadManager.DownloadFile("default.ff", Path.Combine("Original Fast Files", "COD5"));
        }

        /// <summary>
        /// Downloads the patch_mp.ff Fast File for COD5.
        /// </summary>
        private void patchmpffToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            DownloadManager.DownloadFile("patch_mp.ff", Path.Combine("Original Fast Files", "COD5"));
        }

        /// <summary>
        /// Downloads the nazi_zombie_factory_patch.ff Fast File for COD5.
        /// </summary>
        private void nazizombiefactorypatchffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DownloadManager.DownloadFile("nazi_zombie_factory_patch.ff", Path.Combine("Original Fast Files", "COD5"));
        }

        /// <summary>
        /// Downloads the patch_mp.ff Fast File for COD4.
        /// </summary>
        private void patchmpffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DownloadManager.DownloadFile("patch_mp.ff", Path.Combine("Original Fast Files", "COD4"));
        }

        /// <summary>
        /// Checks for updates asynchronously.
        /// </summary>
        private void checkForUpdateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            checkForUpdate();
        }

        private async void checkForUpdate()
        {
            try
            {
                var release = await ReleaseChecker.CheckForNewRelease("primetime43", "Call-of-Duty-FastFile-Editor-For-PS3");

                if (release != null)
                {
                    int latestReleaseInt = ReleaseChecker.convertVersionToInt(release.tag_name);
                    int localProgramVersionInt = ReleaseChecker.convertVersionToInt(_programVersion);

                    if (latestReleaseInt > localProgramVersionInt)
                    {
                        DialogResult result = MessageBox.Show(
                            "Current version: " + _programVersion + "\nNew release available: " + release.name + " (" + release.tag_name + ")\nDo you want to download it?",
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
                    byte[] fullFileBytes = File.ReadAllBytes(selectedFilePath);

                    RawFileNode newRawFileNode = RawFileParser.ExtractAllRawFilesSizeAndName(selectedFilePath)[0];

                    string actualDiskFileName = Path.GetFileName(selectedFilePath);
                    string rawFileNameFromHeader = newRawFileNode.FileName;
                    byte[] rawFileContent = newRawFileNode.RawFileBytes;
                    int newFileMaxSize = newRawFileNode.MaxSize;

                    // 1) Check if file name already exists
                    RawFileNode existingNode = _rawFileNodes
                        .FirstOrDefault(node => node.FileName.Equals(rawFileNameFromHeader, StringComparison.OrdinalIgnoreCase));

                    if (existingNode != null)
                    {
                        // if the newFileMaxSize is greater than the existing node's MaxSize,
                        // update the header's size with the new one and write its content
                        try
                        {
                            if (newFileMaxSize > existingNode.MaxSize)
                            {
                                // write the new file content to the zone at the existing offset
                                // and make sure to append the extra length, so it shouldn't overwrite existing
                                // code that comes after it. Also, update the size in the header.
                                RawFileOps.IncreaseSize(_openedFastFile.ZoneFilePath, existingNode, newRawFileNode.RawFileBytes);
                            }
                            else
                            {
                                // write the raw bytes to the zone at the existing offset
                                RawFileOps.UpdateFileContent(_openedFastFile.ZoneFilePath, existingNode, rawFileContent);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to update file: {ex.Message}",
                                "Injection Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                            return;
                        }
                    }
                    else if (existingNode == null)
                    {
                        // It's a brand-new file, not already in the zone
                        try
                        {
                            //test add to pool
                            AssetRecordPoolOps.AddRawFileAssetRecordToPool(_openedFastFile.OpenedFastFileZone, _openedFastFile.ZoneFilePath);

                            // 1) Append it to the decompressed zone
                            //RawFileOps.OldAppendNewRawFile(_openedFastFile.ZoneFilePath, rawFileName, rawFileContent);
                            RawFileOps.AppendNewRawFile(_openedFastFile.ZoneFilePath, rawFileName, rawFileContent);

                            // 2) Re-extract the entire zone so we pick up the newly inserted file
                            _rawFileNodes = RawFileParser.ExtractAllRawFilesSizeAndName(_openedFastFile.ZoneFilePath);

                            // 3) Clear & re-populate the TreeView
                            filesTreeView.Nodes.Clear();
                            LoadRawFilesTreeView();
                            UIManager.SetRawFileTreeNodeColors(filesTreeView);

                            //_hasUnsavedChanges = true;

                            MessageBox.Show(
                                $"File '{rawFileName}' successfully injected into the zone file.",
                                "Injection Complete",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information
                            );
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to inject file: {ex.Message}",
                                "Injection Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error
                            );
                        }
                    }

                    // 2) Re-extract the entire zone to update _rawFileNodes
                    _rawFileNodes = RawFileParser.ExtractAllRawFilesSizeAndName(_openedFastFile.ZoneFilePath);

                    // 3) Clear & re-populate the TreeView to reflect the newly added/updated node
                    filesTreeView.Nodes.Clear();
                    LoadRawFilesTreeView();

                    // Optionally re-apply any color or style
                    UIManager.SetRawFileTreeNodeColors(filesTreeView);

                    // 4) Indicate changes
                    //_hasUnsavedChanges = true;

                    MessageBox.Show(
                        $"File '{rawFileName}' was successfully injected/updated in the zone file.",
                        "Injection Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
            }
        }

        /// <summary>
        /// Exports the selected file, including its header information, to a chosen location.
        /// </summary>
        private void exportFileMenuItem_Click(object sender, EventArgs e)
        {
            if (filesTreeView.SelectedNode == null)
            {
                MessageBox.Show("Please select a file to export.", "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!(filesTreeView.SelectedNode.Tag is RawFileNode selectedFileNode))
            {
                MessageBox.Show("Selected node does not have a valid RawFileNode.", "Invalid Selection", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string fileExtension = Path.GetExtension(selectedFileNode.FileName);
            string validExtensions = string.Join(",", Constants.RawFiles.FileNamePatternStrings);

            RawFileOperations.ExportRawFile(selectedFileNode, fileExtension);
        }

        /// <summary>
        /// Close the opened fast file, clear the tree view, and reset the form.
        /// Recompresses the zone file back into the fast file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            CloseFastFileAndCleanUp(true);
        }

        private void renameFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (filesTreeView.SelectedNode == null)
            {
                MessageBox.Show("Please select a file to rename.", "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!(filesTreeView.SelectedNode.Tag is RawFileNode selectedFileNode))
            {
                MessageBox.Show("Selected node does not have a valid position.", "Invalid Selection", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (selectedFileNode == null)
            {
                MessageBox.Show("Selected file node not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            RawFileOperations.RenameRawFile(filesTreeView, _openedFastFile.FfFilePath, _openedFastFile.ZoneFilePath, _rawFileNodes, _openedFastFile);
        }

        /// <summary>
        /// Populates the DataGridView with Zone decimal values.
        /// </summary>
        private void LoadZoneHeaderValues(Zone zone)
        {
            if (zone == null || zone.DecimalValues == null)
            {
                _openedFastFile.OpenedFastFileZone.SetZoneOffsets();
            }

            // Convert the dictionary to a list of objects with matching property names
            var dataSource = zone.DecimalValues.Select(kvp => new
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

        /// <summary>
        /// Populates the String Table page view with the tables extracted from the zone file.
        /// </summary>
        private void PopulateStringTable()
        {
            if (_stringTables.Count <= 0)
            {
                mainTabControl.TabPages.Remove(stringTablesTabPage); // hide the tab page if there's no data to show
                return;
            }

            // Clear existing nodes
            stringTableTreeView.Nodes.Clear();

            // For each table, create a node
            foreach (var table in _stringTables)
            {
                TreeNode tableNode = new TreeNode(table.TableName)
                {
                    Tag = table // store the StringTable object for later use
                };

                // Add child nodes for Rows and Columns
                tableNode.Nodes.Add($"Rows: {table.RowCount}");
                tableNode.Nodes.Add($"Columns: {table.ColumnCount}");

                // Add an extra node showing where the CSV text was found.
                // For example, you can display the file header start offset.
                tableNode.Nodes.Add($"Found at offset: 0x{table.StartOfFileHeader:X}");

                stringTableTreeView.Nodes.Add(tableNode);
            }
        }

        private void stringTableTreeView_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            // Cancel selection for child nodes with specific text
            if (e.Node.Text.StartsWith("Rows:") ||
                e.Node.Text.StartsWith("Columns:") ||
                e.Node.Text.StartsWith("Found at offset:"))
            {
                e.Cancel = true;
            }
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

        private void CloseFastFileAndCleanUp(bool deleteZoneFile = false)
        {
            try
            {
                if (_openedFastFile != null && File.Exists(_openedFastFile.FfFilePath))
                {
                    FastFileProcessing.RecompressFastFile(_openedFastFile.FfFilePath, _openedFastFile.ZoneFilePath, _openedFastFile);
                    _hasUnsavedChanges = false; // Reset the flag after saving
                    CleanUpAndClearUI();

                    try
                    {
                        // Delete the zone file if requested
                        if (deleteZoneFile)
                            File.Delete(_openedFastFile.ZoneFilePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to delete zone file: {ex.Message}", "Deletion Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    _openedFastFile = null;
                    MessageBox.Show("Fast File closed.", "Close Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to close fastfile: {ex.Message}", "Close Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CleanUpAndClearUI()
        {
            filesTreeView.Nodes.Clear();
            treeViewMapEnt.Nodes.Clear();
            stringTableTreeView.Nodes.Clear();
            tagsListView.Items.Clear();
            zoneInfoDataGridView.DataSource = null;
            textEditorControlEx1.Text = "";
            textEditorControlEx1.ResetText();
            localizeListView.Items.Clear();
            localizeListView.Columns.Clear();
            tagsListView.Columns.Clear();
            assetPoolListView.Items.Clear();
            assetPoolListView.Columns.Clear();

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
        /// Populates the Zone Asset Pool list view from the _zoneAssetRecords
        /// </summary>
        private void LoadAssetPoolIntoListView()
        {
            // Make sure we have a valid zone and a populated asset pool
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

            // Columns that are going to be on the list view
            assetPoolListView.Columns.Add($"Index ({_zoneAssetRecords.Count})", 100);
            assetPoolListView.Columns.Add("Asset Type", 100);
            assetPoolListView.Columns.Add("AssetPoolRecordOffset", 80);
            assetPoolListView.Columns.Add("Header Start", 100);
            assetPoolListView.Columns.Add("Header End", 100);
            assetPoolListView.Columns.Add("Data Start", 100);
            assetPoolListView.Columns.Add("Data End", 100);
            assetPoolListView.Columns.Add("Size", 60);
            assetPoolListView.Columns.Add("Asset Record End", 100);
            assetPoolListView.Columns.Add("Name", 120);
            assetPoolListView.Columns.Add("Parsing Method", 200);

            // Place the asset pool itself at the top
            var pool = new ListViewItem("");
            pool.SubItems.Add("Asset Pool");
            pool.SubItems.Add(string.Empty);
            pool.SubItems.Add(string.Empty);
            pool.SubItems.Add(string.Empty);
            pool.SubItems.Add($"0x{_assetPoolStartOffset:X}");
            pool.SubItems.Add($"0x{_assetPoolEndOffset:X}");
            pool.SubItems.Add($"0x{(_assetPoolEndOffset - _assetPoolStartOffset):X}");

            assetPoolListView.Items.Add(pool);


            // Populate a row (ListViewItem) for each record
            int index = 1;
            foreach (var record in _zoneAssetRecords)
            {
                var lvi = new ListViewItem(index.ToString());

                // First column: AssetType
                lvi.SubItems.Add(record.AssetType.ToString());

                // Second column: AssetPoolRecordOffset in hex
                lvi.SubItems.Add($"0x{record.AssetPoolRecordOffset:X}");

                // Third & Fourth columns
                lvi.SubItems.Add($"0x{record.HeaderStartOffset:X}");
                lvi.SubItems.Add($"0x{record.HeaderEndOffset:X}");

                // Fifth & Sixth columns
                lvi.SubItems.Add($"0x{record.AssetDataStartPosition:X}");
                lvi.SubItems.Add($"0x{record.AssetDataEndOffset:X}");

                // Seventh column
                lvi.SubItems.Add($"0x{record.Size:X}");

                // Eighth column
                lvi.SubItems.Add($"0x{record.AssetRecordEndOffset:X}");

                // Ninth column
                lvi.SubItems.Add(record.Name ?? string.Empty);

                // Tenth column: Entire content, no truncation
                if (!string.IsNullOrEmpty(record.AdditionalData))
                {
                    lvi.SubItems.Add(record.AdditionalData);
                }
                else
                {
                    //lvi.SubItems.Add(string.Empty);
                    lvi.SubItems.Add("Unable to read.");
                }

                // Finally, add the row to the list
                assetPoolListView.Items.Add(lvi);
                index++;
            }

            // Auto-resize columns to fit header size or content
            assetPoolListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void stringTableTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // 1) Clear existing items & columns in the ListView
            stringTableListView.Items.Clear();
            stringTableListView.Columns.Clear();

            // 2) Use "Details" view with full-row select
            stringTableListView.View = View.Details;
            stringTableListView.FullRowSelect = true;
            stringTableListView.GridLines = true;

            // 3) Define columns for Index, Offset, and Text
            //    Feel free to rename or adjust widths as desired
            stringTableListView.Columns.Add("Index", 60);       // Which cell # in the table
            stringTableListView.Columns.Add("Offset (Hex)", 100); // The file offset (if you want it)
            stringTableListView.Columns.Add("Text", 300);

            // Make sure the selected node actually corresponds to a StringTable
            if (e.Node?.Tag is StringTable selectedTable)
            {
                // 4) Populate one row per cell in "Cells"
                for (int i = 0; i < selectedTable?.Cells?.Count; i++)
                {
                    // Each entry in "Cells" is (Offset, Text)
                    var (offset, text) = selectedTable.Cells[i];

                    // Create a new ListViewItem for this cell
                    ListViewItem lvi = new ListViewItem(i.ToString());        // 1st column: Index
                    lvi.SubItems.Add($"0x{offset:X}");                        // 2nd column: Offset in hex
                    lvi.SubItems.Add(text);                                   // 3rd column: the cell text

                    stringTableListView.Items.Add(lvi);
                }
            }

            // 5) Auto-resize columns to fit headers or content
            stringTableListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        /// <summary>
        /// Extracts all raw files, including header information, to a chosen folder.
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

                        // Write the raw bytes to the destination file.
                        // (Assumes that rawFileNode.RawFileBytes includes header info.)
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

        private void tESTAddRawFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AssetRecordPoolOps.AddRawFileAssetRecordToPool(_openedFastFile.OpenedFastFileZone, _openedFastFile.ZoneFilePath);
        }
    }
}
