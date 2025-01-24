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
                    rawFileNodes = FastFileProcessing.ExtractZoneFileEntriesWithSizeAndName(_openedFastFile.ZoneFilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to decompress FastFile: {ex.Message}", "Decompression Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    _openedFastFile.OpenedFastFilesZone.FileData = File.ReadAllBytes(_openedFastFile.ZoneFilePath);
                    _openedFastFile.OpenedFastFilesZone.SetZoneOffsets();
                    PopulateTreeView();
                    PopulateZoneValuesDataGridView(_openedFastFile.OpenedFastFilesZone);
                    PopulateStringTable();
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
                Application.Restart();
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
                        Application.Restart();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to save Fast File As: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Cleans up resources when the form is closed.
        /// </summary>
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                // Deleting the zone file of the opened ff file
                if (_openedFastFile != null && File.Exists(_openedFastFile.ZoneFilePath))
                {
                    File.Delete(_openedFastFile.ZoneFilePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete zone file: {ex.Message}", "Deletion Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Exits the application.
        /// </summary>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
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
            try
            {
                if (_openedFastFile != null && File.Exists(_openedFastFile.FfFilePath))
                {
                    FastFileProcessing.RecompressFastFile(_openedFastFile.FfFilePath, _openedFastFile.ZoneFilePath, _openedFastFile);
                    _hasUnsavedChanges = false; // Reset the flag after saving
                    filesTreeView.Nodes.Clear();
                    textEditorControl1.ResetText();
                    MessageBox.Show("Fast File closed.", "Close Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to close fastfile: {ex.Message}", "Close Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                ZoneOffset = _openedFastFile.OpenedFastFilesZone.GetZoneOffset(kvp.Key)
            }).ToList();

            // Assign the data source to the DataGridView
            dataGridView1.DataSource = dataSource;
        }

        /// <summary>
        /// Populates the Tags page view with the tags extracted from the zone file.
        /// </summary>
        private void PopulateTags()
        {

        }

        /// <summary>
        /// Populates the String Table page view with the tables extracted from the zone file.
        /// </summary>
        private void PopulateStringTable()
        {
            // Clear the existing nodes to avoid duplicates
            stringTablesTreeView.Nodes.Clear();

            // 1) Find all CSV string tables in the zone
            List<ZoneStringTable> csvTables = StringTableOperations.FindCsvStringTables(_openedFastFile.OpenedFastFilesZone);
            if (csvTables == null || csvTables.Count == 0)
                return;

            // 2) Add each table to the TreeView
            foreach (var table in csvTables)
            {
                // Create a parent node with the table name
                TreeNode tableNode = new TreeNode(table.TableName);

                // Add child nodes for RowCount and ColumnCount
                tableNode.Nodes.Add("Rows: " + table.RowCount);
                tableNode.Nodes.Add("Columns: " + table.ColumnCount);

                // Add this table node to the top-level tree
                stringTablesTreeView.Nodes.Add(tableNode);
            }
        }
    }
}
