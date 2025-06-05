﻿using Call_of_Duty_FastFile_Editor.CodeOperations;
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

        /// <summary>
        /// List of string tables extracted from the zone file.
        /// </summary>
        private List<StringTable> _stringTables;

        private List<LocalizedEntry> _localizedEntries;

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
        /// </summary>
        private void saveFastFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                _fastFileHandler?.Recompress(_openedFastFile.FfFilePath, _openedFastFile.ZoneFilePath, _openedFastFile);
                MessageBox.Show("Fast File saved to:\n\n" + _openedFastFile.FfFilePath,
                                "Saved",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Asterisk);

                // Reset every node’s dirty flag now that the zone is saved
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
        /// </summary>
        private void saveFastFileAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Fast Files (*.ff)|*.ff|All Files (*.*)|*.*";
                saveFileDialog.Title = "Save Fast File As";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string newFilePath = saveFileDialog.FileName;
                        _fastFileHandler?.Recompress(_openedFastFile.FfFilePath, newFilePath, _openedFastFile);
                        MessageBox.Show("Fast File saved to:\n\n" + newFilePath,
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
            SaveCloseFastFileAndCleanUp(true);
            Close();
        }

        /// <summary>
        /// Saves the raw file using SaveRawFile utility.
        /// </summary>
        private void saveRawFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selectedNode = GetSelectedRawFileNode();
            if (selectedNode == null) return;

            _rawFileService.SaveZoneRawFileChanges(
                filesTreeView,
                _openedFastFile.FfFilePath,
                _openedFastFile.ZoneFilePath,
                _rawFileNodes,
                textEditorControlEx1.Text,
                _openedFastFile
            );

            selectedNode.HasUnsavedChanges = false;

            // >>> Immediately update the status strip after saving <<<
            UIManager.UpdateStatusStrip(
                selectedFileMaxSizeStatusLabel,
                selectedFileCurrentSizeStatusLabel,
                selectedNode.MaxSize,
                textEditorControlEx1.Text.Length
            );

            RefreshZoneData();
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
                var release = await ReleaseChecker.CheckForNewRelease("primetime43", "Call-of-Duty-FastFile-Editor-For-PS3");

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

                    // Parse the file to obtain expected header details.
                    RawFileNode newRawFileNode = RawFileParser.ExtractAllRawFilesSizeAndName(selectedFilePath)[0];
                    string rawFileNameFromHeader = newRawFileNode.FileName;
                    byte[] rawFileContent = newRawFileNode.RawFileBytes;
                    int newFileMaxSize = newRawFileNode.MaxSize;

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

                            var selectedNode = GetSelectedRawFileNode();
                            if (selectedNode == null) return;

                            // >>> Immediately update the status strip after saving <<<
                            UIManager.UpdateStatusStrip(
                                selectedFileMaxSizeStatusLabel,
                                selectedFileCurrentSizeStatusLabel,
                                selectedNode.MaxSize,
                                textEditorControlEx1.Text.Length
                            );
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
                        try
                        {
                            // Add a new asset record entry.
                            AssetRecordPoolOps.AddRawFileAssetRecordToPool(_openedFastFile.OpenedFastFileZone, _openedFastFile.ZoneFilePath);
                            // Inject new file using the new AppendNewRawFile overload.
                            _rawFileService.AppendNewRawFile(_openedFastFile.ZoneFilePath, selectedFilePath, newFileMaxSize);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to inject file: {ex.Message}",
                                "Injection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }

                    RefreshZoneData();
                    ReloadAllRawFileNodesAndUI();
                    MessageBox.Show($"File '{rawFileName}' was successfully injected & saved in the zone file.",
                        "Injection Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        /// <summary>
        /// Exports the selected file, including its header information, to a chosen location.
        /// </summary>
        private void exportFileMenuItem_Click(object sender, EventArgs e)
        {
            RawFileNode selectedNode = GetSelectedRawFileNode();
            if (selectedNode == null)
                return;

            string fileExtension = Path.GetExtension(selectedNode.FileName);
            string validExtensions = string.Join(",", RawFileConstants.FileNamePatternStrings);

            _rawFileService.ExportRawFile(selectedNode, fileExtension);
        }

        /// <summary>
        /// Close the opened fast file, clear the tree view, and reset the form.
        /// Recompresses the zone file back into the fast file. (saves changes)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeFastFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveCloseFastFileAndCleanUp(true);
        }

        private void renameFileToolStripMenuItem_Click(object sender, EventArgs e)
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

        private void SaveCloseFastFileAndCleanUp(bool deleteZoneFile = false)
        {
            try
            {
                if (_openedFastFile != null && File.Exists(_openedFastFile.FfFilePath))
                {
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
                    MessageBox.Show("Fast File Saved & Closed.", "Close Complete",
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
                if (_openedFastFile.IsCod4File)
                    lvi.SubItems.Add(record.AssetType_COD4.ToString());
                else
                    lvi.SubItems.Add(record.AssetType_COD5.ToString());

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

        // move this eventually maybe
        private void RefreshZoneData()
        {
            if (_openedFastFile == null)
                return;

            // 1) Fully re-read the zone file bytes from disk
            _openedFastFile.OpenedFastFileZone.LoadData();

            // 2) Re-parse the asset pool (start/end offsets, record offsets, etc.)
            _openedFastFile.OpenedFastFileZone.ParseAssetPool();

            // 3) Re-run your asset record processing logic
            //    This updates _rawFileNodes, _zoneAssetRecords, _stringTables, etc.
            LoadAssetRecordsData();

            // 4) Rebuild the entire UI
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
            stringTableListView.Items.Clear();
            stringTableListView.Columns.Clear();
            stringTableTreeView.Nodes.Clear();
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

                // Clean up the temp zone file
                if (_openedFastFile != null && File.Exists(_openedFastFile.ZoneFilePath))
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
                    try
                    {
                        _rawFileService.AdjustRawFileNodeSize(_openedFastFile.ZoneFilePath, selectedNode, newSize);
                        MessageBox.Show($"File '{selectedNode.FileName}' size increased to {newSize} bytes successfully.",
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

            try
            {
                _openedFastFile = new FastFile(openFileDialog.FileName);
                UIManager.UpdateLoadedFileNameStatusStrip(loadedFileNameStatusLabel, _openedFastFile.FastFileName, _openedFastFile.IsCod4File);
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

                    // 3) Parse the asset pool out of the newly‐loaded zone
                    _openedFastFile.OpenedFastFileZone.ParseAssetPool();

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

            try
            {
                _openedFastFile = new FastFile(openFileDialog.FileName);
                UIManager.UpdateLoadedFileNameStatusStrip(loadedFileNameStatusLabel, _openedFastFile.FastFileName, _openedFastFile.IsCod4File);
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

                    // 3) Parse the asset pool out of the newly‐loaded zone
                    _openedFastFile.OpenedFastFileZone.ParseAssetPool();

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

        // There's a lot of duplicate code around this issue. This needs revisited & fixed/cleaned up
        private void ReloadAllRawFileNodesAndUI()
        {
            // Reparse the raw file nodes from disk
            _rawFileNodes = RawFileParser.ExtractAllRawFilesSizeAndName(_openedFastFile.ZoneFilePath);
            RawFileNode.CurrentZone = _openedFastFile.OpenedFastFileZone;

            // Rebuild UI for files list
            LoadRawFilesTreeView();
        }

    }
}
