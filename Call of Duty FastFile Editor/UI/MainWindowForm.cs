using Call_of_Duty_FastFile_Editor.CodeOperations;
using Call_of_Duty_FastFile_Editor.IO;
using Call_of_Duty_FastFile_Editor.Models;
using Call_of_Duty_FastFile_Editor.UI;
using System.Text.RegularExpressions;
using Call_of_Duty_FastFile_Editor.Original_Fast_Files;
using System.Diagnostics;
using static Call_of_Duty_FastFile_Editor.Service.GitHubReleaseChecker;
using System.Text;
using System.Windows.Forms;
using System.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

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
        /// Path to the selected Fast File. (contains the full path + file name + extension)
        /// </summary>
        private string ffFilePath;

        /// <summary>
        /// Path to the decompressed zone file corresponding to the selected Fast File.
        /// </summary>
        private string zoneFilePath;

        /// <summary>
        /// List of file entry nodes extracted from the zone file.
        /// </summary>
        private List<RawFileNode> rawFileNodes;

        /// <summary>
        /// Header information of the opened Fast File.
        /// </summary>
        private FastFileHeader _header;

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

            ffFilePath = openFileDialog.FileName;
            zoneFilePath = Path.Combine(Path.GetDirectoryName(ffFilePath), Path.GetFileNameWithoutExtension(ffFilePath) + ".zone");

            UIManager.UpdateLoadedFileNameStatusStrip(loadedFileNameStatusLabel, ffFilePath);

            try
            {
                _header = new FastFileHeader(ffFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to read Fast File header: {ex.Message}", "Header Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_header.IsValid)
            {
                FastFileProcessing.DecompressFastFile(ffFilePath, zoneFilePath);
                rawFileNodes = FastFileProcessing.ExtractFileEntriesWithSizeAndName(zoneFilePath);
                PopulateTreeView();
            }
            else
            {
                MessageBox.Show("Invalid Fast File!\n\nThe Fast File you have selected is not a valid PS3 .ff!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
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
                            SaveRawFile.Save(
                                filesTreeView,              // TreeView control
                                ffFilePath,                 // Path to the Fast File (.ff)
                                zoneFilePath,               // Path to the decompressed zone file
                                rawFileNodes,             // List of RawFileNode objects
                                textEditorControl1.Text,    // Updated text from the editor
                                _header                     // FastFileHeader instance
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
                FastFileProcessing.RecompressFastFile(ffFilePath, zoneFilePath, _header);
                MessageBox.Show("Fast File saved to:\n\n" + ffFilePath, "Saved", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
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
                        FastFileProcessing.RecompressFastFile(ffFilePath, newFilePath, _header);
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
            // Deleting the zone file of the opened ff file
            if (File.Exists(zoneFilePath))
            {
                try
                {
                    File.Delete(zoneFilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete zone file: {ex.Message}", "Deletion Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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
                SaveRawFile.Save(
                    filesTreeView,                // TreeView control
                    ffFilePath,                   // Path to the Fast File (.ff)
                    zoneFilePath,                 // Path to the decompressed zone file
                    rawFileNodes,               // List of RawFileNode objects
                    textEditorControl1.Text,      // Updated text from the editor
                    _header                       // FastFileHeader instance
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
                    string fileName = Path.GetFileName(selectedFilePath);
                    byte[] fileBytes = File.ReadAllBytes(selectedFilePath);

                    // Check if the file already exists in the zone
                    RawFileNode existingNode = rawFileNodes.FirstOrDefault(node => node.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));

                    if (existingNode != null)
                    {
                        // Overwrite existing raw file
                        /*if (fileBytes.Length > existingNode.MaxSize)
                        {
                            MessageBox.Show($"The file size exceeds the maximum allowed size of {existingNode.MaxSize} bytes.", "Injection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }*/

                        try
                        {
                            FastFileProcessing.UpdateFileContent(zoneFilePath, existingNode, fileBytes);
                            MessageBox.Show($"File '{fileName}' successfully updated in the zone file.", "Injection Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to update file: {ex.Message}", "Injection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        // Update TreeView
                        TreeNode existingTreeNode = filesTreeView.Nodes.Cast<TreeNode>()
                            .FirstOrDefault(n => n.Text.Equals(fileName, StringComparison.OrdinalIgnoreCase));
                        if (existingTreeNode != null)
                        {
                            existingTreeNode.Tag = existingNode.PatternIndexPosition;
                        }
                    }
                    else
                    {
                        // Inject as a new raw file
                        RawFileNode newNode = new RawFileNode
                        {
                            FileName = fileName,
                            RawFileBytes = fileBytes,
                            RawFileContent = Encoding.Default.GetString(fileBytes),
                            MaxSize = fileBytes.Length,
                            PatternIndexPosition = rawFileNodes.Count > 0 ? rawFileNodes.Max(node => node.PatternIndexPosition) + 1 : 0,
                            StartOfFileHeader = rawFileNodes.Count > 0 ? rawFileNodes.Max(node => node.CodeEndPosition) : 0
                        };

                        try
                        {
                            // Write the new file to the zone file
                            using (FileStream fs = new FileStream(zoneFilePath, FileMode.Open, FileAccess.Write))
                            {
                                fs.Seek(newNode.StartOfFileHeader, SeekOrigin.Begin);
                                fs.Write(newNode.Header, 0, newNode.Header.Length);
                                fs.Write(newNode.RawFileBytes, 0, newNode.RawFileBytes.Length);
                            }

                            rawFileNodes.Add(newNode);

                            // Add the new node to TreeView
                            TreeNode newTreeNode = new TreeNode(newNode.FileName)
                            {
                                Tag = newNode.PatternIndexPosition
                            };
                            filesTreeView.Nodes.Add(newTreeNode);

                            MessageBox.Show($"File '{fileName}' successfully injected into the zone file.", "Injection Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to inject file: {ex.Message}", "Injection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }

                    // Mark changes as unsaved
                    _hasUnsavedChanges = true;
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

            // Validate the extension
            if (!validExtensions.Split(',').Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
            {
                MessageBox.Show($"Unsupported file extension: {fileExtension}.", "Invalid Extension", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Title = "Export File";
                saveFileDialog.FileName = SanitizeFileName(selectedFileNode.FileName);
                saveFileDialog.Filter = $"{fileExtension.TrimStart('.').ToUpper()} Files (*{fileExtension})|*{fileExtension}|All Files (*.*)|*.*";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string exportPath = saveFileDialog.FileName;

                        using (FileStream fs = new FileStream(exportPath, FileMode.Create, FileAccess.Write))
                        using (BinaryWriter bw = new BinaryWriter(fs))
                        {
                            // Write the header (size and padding)
                            bw.Write(selectedFileNode.Header);

                            // Write the file name in ASCII
                            byte[] fileNameBytes = Encoding.ASCII.GetBytes(selectedFileNode.FileName);
                            bw.Write(fileNameBytes);

                            // Write a null terminator (0x00) after the file name
                            bw.Write((byte)0x00);

                            // Write the file content
                            byte[] contentBytes = selectedFileNode.RawFileBytes ?? Encoding.UTF8.GetBytes(selectedFileNode.RawFileContent ?? string.Empty);
                            bw.Write(contentBytes);

                            // Write padding (00 FF FF FF FF) at the end
                            bw.Write(new byte[] { 0x00, 0xFF, 0xFF, 0xFF, 0xFF });
                        }

                        MessageBox.Show($"File successfully exported to:\n\n{exportPath}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to export file: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Sanitizes a filename by replacing invalid characters with underscores.
        /// </summary>
        /// <param name="fileName">The original filename.</param>
        /// <returns>A sanitized filename safe for the filesystem.</returns>
        private string SanitizeFileName(string fileName)
        {
            // Retrieve all invalid characters for filenames
            char[] invalidChars = Path.GetInvalidFileNameChars();

            // Replace each invalid character with an underscore
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }

            return fileName;
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
                FastFileProcessing.RecompressFastFile(ffFilePath, zoneFilePath, _header);
                _hasUnsavedChanges = false; // Reset the flag after saving
                Application.Restart();
            }
            catch {}
        }
    }
}
