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
using System;
using System.ComponentModel;

namespace Call_of_Duty_FastFile_Editor
{
    public partial class MainWindowForm : Form
    {
        private string _programVersion = "v1.0.0";
        private string _originalFastFilesPath = Path.Combine(Application.StartupPath, "Original Fast Files");
        private TreeNode _previousSelectedNode;
        private bool _hasUnsavedChanges = false;

        public MainWindowForm()
        {
            InitializeComponent();
            textEditorControl1.SetHighlighting("C#");

            DirectoryInfo directoryInfo = new DirectoryInfo(_originalFastFilesPath);
            directoryInfo.Attributes |= FileAttributes.Hidden;
            this.Text = $"Call of Duty Fast File Editor for PS3 - {_programVersion}";

            // Universal toolstrip menu item
            copyToolStripMenuItem.Click += copyToolStripMenuItem_Click;
            universalContextMenu.Opening += universalContextMenu_Opening;
        }

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

        /// <summary>
        /// List of file entry nodes extracted from the zone file.
        /// </summary>
        private List<RawFileNode> rawFileNodes;

        /// <summary>
        /// FastFile instance representing the opened Fast File.
        /// </summary>
        private FastFile _openedFastFile;

        /// <summary>
        /// Opens a Fast File, decompresses it, extracts file entries, and populates the TreeView.
        /// </summary>
        private void openFastFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(_openedFastFile != null)
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

            filesTreeView.Nodes.Clear();

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
                    _openedFastFile.OpenedFastFileZone.ZoneFileAssets.RawFiles = FastFileProcessing.ExtractZoneFileEntriesWithSizeAndName(_openedFastFile.ZoneFilePath);
                    rawFileNodes = _openedFastFile.OpenedFastFileZone.ZoneFileAssets.RawFiles;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to decompress FastFile: {ex.Message}", "Decompression Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    _openedFastFile.OpenedFastFileZone.SetZoneData();
                    _openedFastFile.OpenedFastFileZone.SetZoneOffsets();
                    // Move these eventually and change how they're loaded
                    PopulateTreeView();
                    PopulateZoneValuesDataGridView(_openedFastFile.OpenedFastFileZone);
                    PopulateTags();
                    PopulateStringTable();
                    PopulateMapEntities();
                }
                catch (EndOfStreamException ex)
                {
                    MessageBox.Show($"Deserialization failed: {ex.Message}", "Deserialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to deserialize zone file: {ex.Message}", "Deserialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else
            {
                MessageBox.Show("Invalid FastFile!\n\nThe FastFile you have selected is not a valid PS3 .ff!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }

            UIManager.SetTreeNodeColors(filesTreeView);

            // Enable relevant menu items
            saveRawFileToolStripMenuItem.Enabled = true;
            renameRawFileToolStripMenuItem.Enabled = true;
            saveFastFileToolStripMenuItem.Enabled = true;
            saveFastFileAsToolStripMenuItem.Enabled = true;
        }

        /// <summary>
        /// Populates the TreeView with TreeNodes corresponding to RawFileNodes.
        /// </summary>
        private void PopulateTreeView()
        {
            // Clear existing nodes to avoid duplication
            filesTreeView.Nodes.Clear();

            var treeNodes = rawFileNodes.Select(node =>
            {
                var treeNode = new TreeNode(node.FileName)
                {
                    Tag = node.PatternIndexPosition // Associate TreeNode with RawFileNode via Tag
                };
                return treeNode;
            }).ToArray();

            filesTreeView.Nodes.AddRange(treeNodes);
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
                    if (_previousSelectedNode != null)
                    {
                        var previousSelectedNodeData = rawFileNodes
                            .FirstOrDefault(node => node.PatternIndexPosition == (int)_previousSelectedNode.Tag);

                        if (previousSelectedNodeData != null)
                        {
                            RawFileOperations.Save(
                                filesTreeView,              // TreeView control
                                _openedFastFile.FfFilePath,                 // Path to the Fast File (.ff)
                                _openedFastFile.ZoneFilePath,               // Path to the decompressed zone file
                                rawFileNodes,             // List of RawFileNode objects
                                textEditorControl1.Text,    // Updated text from the editor
                                _openedFastFile                     // FastFile instance
                            );
                        }
                    }
                    _hasUnsavedChanges = false;
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true; // Cancel the selection change
                    return;
                }
            }
            _previousSelectedNode = filesTreeView.SelectedNode; // Save the current node before changing
        }

        /// <summary>
        /// Handles actions after selecting a new TreeView node, loading the corresponding file content.
        /// </summary>
        private void filesTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag is int position)
            {
                string fileName = e.Node.Text; // Get the selected file name
                var selectedNode = rawFileNodes.FirstOrDefault(node => node.PatternIndexPosition == position);
                int maxSize = selectedNode?.MaxSize ?? 0;

                if (selectedNode != null)
                {
                    string fileContent = selectedNode.RawFileContent ?? string.Empty;
                    textEditorControl1.TextChanged -= textEditorControl1_TextChanged; // Unsubscribe to prevent multiple triggers
                    textEditorControl1.Text = fileContent;
                    textEditorControl1.TextChanged += textEditorControl1_TextChanged; // Resubscribe

                    UIManager.UpdateSelectedFileStatusStrip(selectedItemStatusLabel, fileName);
                    UIManager.UpdateStatusStrip(
                        selectedFileMaxSizeStatusLabel,
                        selectedFileCurrentSizeStatusLabel,
                        maxSize,
                        textEditorControl1.Text.Length
                    );
                    _hasUnsavedChanges = false; // Reset the flag after loading new content
                }
            }
        }

        /// <summary>
        /// Handles text changes in the editor, marking the content as unsaved.
        /// </summary>
        private void textEditorControl1_TextChanged(object sender, EventArgs e)
        {
            if (filesTreeView.SelectedNode?.Tag is int position)
            {
                var selectedNode = rawFileNodes.FirstOrDefault(node => node.PatternIndexPosition == position);
                if (selectedNode != null)
                {
                    int maxSize = selectedNode.MaxSize;
                    UIManager.UpdateStatusStrip(
                        selectedFileMaxSizeStatusLabel,
                        selectedFileCurrentSizeStatusLabel,
                        maxSize,
                        textEditorControl1.Text.Length
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
                    rawFileNodes,               // List of RawFileNode objects
                    textEditorControl1.Text,      // Updated text from the editor
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
                textEditorControl1.Text = CommentRemover.RemoveCStyleComments(textEditorControl1.Text);
                textEditorControl1.Text = CommentRemover.RemoveCustomComments(textEditorControl1.Text);
                textEditorControl1.Text = Regex.Replace(textEditorControl1.Text, "(\\r\\n){2,}", "\r\n\r\n");
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

            var selectedFileNode = rawFileNodes.FirstOrDefault(node => node.PatternIndexPosition == position);
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
        /// Shows information about the file structure in a new form.
        /// </summary>
        private void fileStructureInfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (filesTreeView.SelectedNode != null)
            {
                if (filesTreeView.SelectedNode.Tag is int position)
                {
                    string fileName = filesTreeView.SelectedNode.Text; // Get the selected file name
                    var selectedFileNode = rawFileNodes.FirstOrDefault(node => node.PatternIndexPosition == position);

                    // Additional logic for handling the selected file node
                    if (selectedFileNode != null)
                    {
                        new FileStructureInfoForm(selectedFileNode).Show();
                    }
                    else
                    {
                        MessageBox.Show("Selected file node not found in file entry nodes.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Selected node does not have a valid position.", "Invalid Selection", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("No node is selected.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Compresses the code in the editor.
        /// </summary>
        private void compressCodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                textEditorControl1.Text = CommentRemover.RemoveCStyleComments(textEditorControl1.Text);
                textEditorControl1.Text = CommentRemover.RemoveCustomComments(textEditorControl1.Text);
                textEditorControl1.Text = CodeCompressor.CompressCode(textEditorControl1.Text);
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
                SyntaxChecker.CheckSyntax(textEditorControl1.Text);
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

                    RawFileNode newRawFileNode = FastFileProcessing.ExtractZoneFileEntriesWithSizeAndName(selectedFilePath)[0];

                    string actualDiskFileName = Path.GetFileName(selectedFilePath);
                    string rawFileNameFromHeader = newRawFileNode.FileName;
                    byte[] rawFileContent = newRawFileNode.RawFileBytes;
                    int newFileMaxSize = newRawFileNode.MaxSize;

                    // 1) Check if file name already exists
                    RawFileNode existingNode = rawFileNodes
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
                                RawFileInject.ExpandAndUpdateFileContent(_openedFastFile.ZoneFilePath, existingNode, newRawFileNode, newRawFileNode.RawFileContent);
                            }
                            else
                            {
                                // write the raw bytes to the zone at the existing offset
                                RawFileInject.UpdateFileContent(_openedFastFile.ZoneFilePath, existingNode, rawFileContent);
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
                            // 1) Append it to the decompressed zone
                            RawFileInject.AppendNewRawFile(_openedFastFile.ZoneFilePath, rawFileName, rawFileContent);

                            // 2) Re-extract the entire zone so we pick up the newly inserted file
                            rawFileNodes = FastFileProcessing.ExtractZoneFileEntriesWithSizeAndName(_openedFastFile.ZoneFilePath);

                            // 3) Clear & re-populate the TreeView
                            filesTreeView.Nodes.Clear();
                            PopulateTreeView();
                            UIManager.SetTreeNodeColors(filesTreeView);

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

                    // 2) Re-extract the entire zone to update rawFileNodes
                    rawFileNodes = FastFileProcessing.ExtractZoneFileEntriesWithSizeAndName(_openedFastFile.ZoneFilePath);

                    // 3) Clear & re-populate the TreeView to reflect the newly added/updated node
                    filesTreeView.Nodes.Clear();
                    PopulateTreeView();

                    // Optionally re-apply any color or style
                    UIManager.SetTreeNodeColors(filesTreeView);

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

            if (!(filesTreeView.SelectedNode.Tag is int position))
            {
                MessageBox.Show("Selected node does not have a valid position.", "Invalid Selection", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            RawFileNode selectedFileNode = rawFileNodes.FirstOrDefault(node => node.PatternIndexPosition == position);
            if (selectedFileNode == null)
            {
                MessageBox.Show("Selected file node not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string fileExtension = Path.GetExtension(selectedFileNode.FileName);
            string validExtensions = ".cfg,.gsc,.str,.vision,.rmb,.csc";

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

            if (!(filesTreeView.SelectedNode.Tag is int position))
            {
                MessageBox.Show("Selected node does not have a valid position.", "Invalid Selection", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            RawFileNode selectedFileNode = rawFileNodes.FirstOrDefault(node => node.PatternIndexPosition == position);
            if (selectedFileNode == null)
            {
                MessageBox.Show("Selected file node not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            RawFileOperations.RenameRawFile(filesTreeView, _openedFastFile.FfFilePath, _openedFastFile.ZoneFilePath, rawFileNodes, _openedFastFile);
        }

        /// <summary>
        /// Populates the DataGridView with Zone decimal values.
        /// </summary>
        private void PopulateZoneValuesDataGridView(Zone zone)
        {
            if (zone == null || zone.DecimalValues == null)
                return;

            // Convert the dictionary to a list of objects with matching property names
            var dataSource = zone.DecimalValues.Select(kvp => new
            {
                ZoneName = kvp.Key,
                ZoneDecValue = kvp.Value,
                ZoneHexValue = Utilities.ConvertToBigEndianHex(kvp.Value),
                ZoneOffset = _openedFastFile.OpenedFastFileZone.GetZoneOffset(kvp.Key)
            }).ToList();

            // Assign the data source to the DataGridView
            dataGridView1.DataSource = dataSource;
        }

        /// <summary>
        /// Populates the Tags page view with the tags extracted from the zone file.
        /// </summary>
        private void PopulateTags()
        {
            tagsListView.View = View.Details;
            tagsListView.Columns.Clear();
            tagsListView.Items.Clear();
            tagsListView.MultiSelect = true;
            tagsListView.FullRowSelect = true;
            tagsListView.Columns.Add("Tag", 100);
            tagsListView.Columns.Add("Offset (Dec)", 100);
            tagsListView.Columns.Add("Offset (Hex)", 100);

            // Fetch the results
            var tagsInfo = TagOperations.FindTags(_openedFastFile.OpenedFastFileZone);

            // Set the opened FastFile's Zone object to hold the tags
            _openedFastFile.OpenedFastFileZone.ZoneFileAssets.Tags = tagsInfo;

            if (tagsInfo == null)
                return;

            // Now tagsInfo.TagEntries holds all entries
            foreach (var entry in tagsInfo.TagEntries)
            {
                // Create a row with 3 columns:
                // 1) Tag
                // 2) Decimal offset
                // 3) Hex offset (with 0x prefix)
                var lvi = new ListViewItem(entry.Tag);

                // Decimal offset
                lvi.SubItems.Add(entry.OffsetDec.ToString());

                // Hex offset (for example "0x1AC4AC0")
                string hexString = $"0x{entry.OffsetDec:X}";
                lvi.SubItems.Add(hexString);

                tagsListView.Items.Add(lvi);
            }

            tagsListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        /// <summary>
        /// Populates the String Table page view with the tables extracted from the zone file.
        /// </summary>
        private void PopulateStringTable()
        {
            // Clear the existing nodes to avoid duplicates
            stringTableTreeView.Nodes.Clear();

            // 1) Find all CSV string tables in the zone
            List<StringTable> csvTables = StringTableOperations.FindCsvStringTables(_openedFastFile.OpenedFastFileZone);
            if (csvTables == null || csvTables.Count == 0)
                return;

            // Set the opened FastFile's Zone object to hold the string tables
            _openedFastFile.OpenedFastFileZone.ZoneFileAssets.StringTables = csvTables;

            // 2) Add each table to the TreeView
            foreach (var table in csvTables)
            {
                // Create a parent node with the table name
                TreeNode tableNode = new TreeNode(table.TableName);

                // Add child nodes for RowCount and ColumnCount
                tableNode.Nodes.Add("Rows: " + table.RowCount);
                tableNode.Nodes.Add("Columns: " + table.ColumnCount);

                // Add this table node to the top-level tree
                stringTableTreeView.Nodes.Add(tableNode);
            }
        }

        private void PopulateMapEntities()
        {
            int offset = MapEntityOperations.FindMapHeaderOffsetViaFF(_openedFastFile.OpenedFastFileZone);
            if (offset == -1)
            {
                //MessageBox.Show("No map header found near large FF blocks.");
                return;
            }

            // Parse entities from that offset
            List<MapEntity> mapTest = MapEntityOperations.ParseMapEntsAtOffset(_openedFastFile.OpenedFastFileZone, offset);

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
            var sizeInfo = MapEntityOperations.GetMapDataSizeAndOffset(_openedFastFile.OpenedFastFileZone, mapTest);
            if (sizeInfo.HasValue)
            {
                // Destructure the tuple
                (int mapSize, int offsetOfSize) = sizeInfo.Value;

                // Create a top-level node for the map size
                TreeNode sizeNode = new TreeNode($"Map Data Size = {mapSize} bytes (dec)");
                treeViewMapEnt.Nodes.Add(sizeNode);

                // Create another node for where that size is stored (in hex)
                string sizeOffsetHex = offsetOfSize.ToString("X");
                TreeNode sizeOffsetNode = new TreeNode($"Map Size Offset = 0x{sizeOffsetHex}");
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
                parentNode.Nodes.Add($"Offset = 0x{offsetHex}");

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
                    _rightClickedItemText = hit.Item.Text;  // or subItem text
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
                    filesTreeView.Nodes.Clear();
                    treeViewMapEnt.Nodes.Clear();
                    stringTableTreeView.Nodes.Clear();
                    tagsListView.Items.Clear();
                    dataGridView1.DataSource = null;
                    textEditorControl1.ResetText();

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
    }
}
