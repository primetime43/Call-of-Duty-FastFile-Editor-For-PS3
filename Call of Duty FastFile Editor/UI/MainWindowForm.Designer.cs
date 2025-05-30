using ICSharpCode.TextEditor;
using ICSharpCode.TextEditorEx;

namespace Call_of_Duty_FastFile_Editor
{
    partial class MainWindowForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            filesTreeView = new TreeView();
            contextMenuStripRawFiles = new ContextMenuStrip(components);
            exportFileMenuItem = new ToolStripMenuItem();
            renameFileToolStripMenuItem = new ToolStripMenuItem();
            increaseFileSizeToolStripMenuItem = new ToolStripMenuItem();
            menuStripTopToolbar = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            openFastFileToolStripMenuItem = new ToolStripMenuItem();
            COD5ToolStripMenuItem = new ToolStripMenuItem();
            cOD4ToolStripMenuItem = new ToolStripMenuItem();
            saveFastFileToolStripMenuItem = new ToolStripMenuItem();
            saveFastFileAsToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem1 = new ToolStripMenuItem();
            exitToolStripMenuItem = new ToolStripMenuItem();
            editToolStripMenuItem = new ToolStripMenuItem();
            saveRawFileToolStripMenuItem = new ToolStripMenuItem();
            renameRawFileToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            compressCodeToolStripMenuItem = new ToolStripMenuItem();
            removeCommentsToolStripMenuItem = new ToolStripMenuItem();
            checkSyntaxToolStripMenuItem = new ToolStripMenuItem();
            toolsToolStripMenuItem = new ToolStripMenuItem();
            injectFileToolStripMenuItem = new ToolStripMenuItem();
            rawFileToolsMenuItem = new ToolStripMenuItem();
            increaseRawFileSizeToolStripMenuItem = new ToolStripMenuItem();
            searchRawFileTxtMenuItem = new ToolStripMenuItem();
            extractAllRawFilesToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem2 = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            CheckForUpdatesToolStripMenuItem = new ToolStripMenuItem();
            splitContainer1 = new SplitContainer();
            textEditorControlEx1 = new TextEditorControlEx();
            statusStripBottom = new StatusStrip();
            loadedFileNameStatusLabel = new ToolStripStatusLabel();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            selectedItemStatusLabel = new ToolStripStatusLabel();
            selectedFileMaxSizeStatusLabel = new ToolStripStatusLabel();
            selectedFileCurrentSizeStatusLabel = new ToolStripStatusLabel();
            filesTreeToolTip = new ToolTip(components);
            mainTabControl = new TabControl();
            universalContextMenu = new ContextMenuStrip(components);
            copyToolStripMenuItem = new ToolStripMenuItem();
            rawFilesPage = new TabPage();
            stringTablesTabPage = new TabPage();
            stringTableListView = new ListView();
            stringTableTreeView = new TreeView();
            collision_Map_AssetTabPage = new TabPage();
            treeViewMapEnt = new TreeView();
            localizeTabPage = new TabPage();
            localizeListView = new ListView();
            tagsTabPage = new TabPage();
            tagsListView = new ListView();
            assetPoolTabPage = new TabPage();
            assetPoolListView = new ListView();
            zoneFileTabPage = new TabPage();
            zoneInfoDataGridView = new DataGridView();
            bindingSource1 = new BindingSource(components);
            contextMenuStripRawFiles.SuspendLayout();
            menuStripTopToolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            statusStripBottom.SuspendLayout();
            mainTabControl.SuspendLayout();
            universalContextMenu.SuspendLayout();
            rawFilesPage.SuspendLayout();
            stringTablesTabPage.SuspendLayout();
            collision_Map_AssetTabPage.SuspendLayout();
            localizeTabPage.SuspendLayout();
            tagsTabPage.SuspendLayout();
            assetPoolTabPage.SuspendLayout();
            zoneFileTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)zoneInfoDataGridView).BeginInit();
            ((System.ComponentModel.ISupportInitialize)bindingSource1).BeginInit();
            SuspendLayout();
            // 
            // filesTreeView
            // 
            filesTreeView.BackColor = SystemColors.ScrollBar;
            filesTreeView.ContextMenuStrip = contextMenuStripRawFiles;
            filesTreeView.Dock = DockStyle.Fill;
            filesTreeView.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            filesTreeView.HideSelection = false;
            filesTreeView.Location = new Point(8, 0);
            filesTreeView.Name = "filesTreeView";
            filesTreeView.Size = new Size(330, 743);
            filesTreeView.TabIndex = 0;
            filesTreeToolTip.SetToolTip(filesTreeView, "Right click for more options.");
            filesTreeView.BeforeSelect += filesTreeView_BeforeSelect;
            filesTreeView.AfterSelect += filesTreeView_AfterSelect;
            // 
            // contextMenuStripRawFiles
            // 
            contextMenuStripRawFiles.Items.AddRange(new ToolStripItem[] { exportFileMenuItem, renameFileToolStripMenuItem, increaseFileSizeToolStripMenuItem });
            contextMenuStripRawFiles.Name = "contextMenuStrip1";
            contextMenuStripRawFiles.Size = new Size(162, 70);
            // 
            // exportFileMenuItem
            // 
            exportFileMenuItem.Name = "exportFileMenuItem";
            exportFileMenuItem.Size = new Size(161, 22);
            exportFileMenuItem.Text = "Export File";
            exportFileMenuItem.ToolTipText = "Export the selected raw file";
            exportFileMenuItem.Click += exportFileMenuItem_Click;
            // 
            // renameFileToolStripMenuItem
            // 
            renameFileToolStripMenuItem.Name = "renameFileToolStripMenuItem";
            renameFileToolStripMenuItem.Size = new Size(161, 22);
            renameFileToolStripMenuItem.Text = "Rename File";
            renameFileToolStripMenuItem.Click += renameFileToolStripMenuItem_Click;
            // 
            // increaseFileSizeToolStripMenuItem
            // 
            increaseFileSizeToolStripMenuItem.Name = "increaseFileSizeToolStripMenuItem";
            increaseFileSizeToolStripMenuItem.Size = new Size(161, 22);
            increaseFileSizeToolStripMenuItem.Text = "Increase File Size";
            increaseFileSizeToolStripMenuItem.Click += increaseFileSizeToolStripMenuItem_Click;
            // 
            // menuStripTopToolbar
            // 
            menuStripTopToolbar.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            menuStripTopToolbar.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, editToolStripMenuItem, toolsToolStripMenuItem, helpToolStripMenuItem });
            menuStripTopToolbar.Location = new Point(0, 0);
            menuStripTopToolbar.Name = "menuStripTopToolbar";
            menuStripTopToolbar.Size = new Size(1450, 28);
            menuStripTopToolbar.TabIndex = 1;
            menuStripTopToolbar.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openFastFileToolStripMenuItem, saveFastFileToolStripMenuItem, saveFastFileAsToolStripMenuItem, toolStripMenuItem1, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(44, 24);
            fileToolStripMenuItem.Text = "File";
            // 
            // openFastFileToolStripMenuItem
            // 
            openFastFileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { COD5ToolStripMenuItem, cOD4ToolStripMenuItem });
            openFastFileToolStripMenuItem.Name = "openFastFileToolStripMenuItem";
            openFastFileToolStripMenuItem.Size = new Size(286, 24);
            openFastFileToolStripMenuItem.Text = "Open Fast File";
            // 
            // COD5ToolStripMenuItem
            // 
            COD5ToolStripMenuItem.Name = "COD5ToolStripMenuItem";
            COD5ToolStripMenuItem.Size = new Size(117, 24);
            COD5ToolStripMenuItem.Text = "COD5";
            COD5ToolStripMenuItem.Click += COD5ToolStripMenuItem_Click;
            // 
            // cOD4ToolStripMenuItem
            // 
            cOD4ToolStripMenuItem.Name = "cOD4ToolStripMenuItem";
            cOD4ToolStripMenuItem.Size = new Size(117, 24);
            cOD4ToolStripMenuItem.Text = "COD4";
            cOD4ToolStripMenuItem.Click += cOD4ToolStripMenuItem_Click;
            // 
            // saveFastFileToolStripMenuItem
            // 
            saveFastFileToolStripMenuItem.Enabled = false;
            saveFastFileToolStripMenuItem.Name = "saveFastFileToolStripMenuItem";
            saveFastFileToolStripMenuItem.Size = new Size(286, 24);
            saveFastFileToolStripMenuItem.Text = "Save Fast File (Recompress)";
            saveFastFileToolStripMenuItem.ToolTipText = "Saves changes to the FF";
            saveFastFileToolStripMenuItem.Click += saveFastFileToolStripMenuItem_Click;
            // 
            // saveFastFileAsToolStripMenuItem
            // 
            saveFastFileAsToolStripMenuItem.Enabled = false;
            saveFastFileAsToolStripMenuItem.Name = "saveFastFileAsToolStripMenuItem";
            saveFastFileAsToolStripMenuItem.Size = new Size(286, 24);
            saveFastFileAsToolStripMenuItem.Text = "Save Fast File as... (Recompress)";
            saveFastFileAsToolStripMenuItem.Click += saveFastFileAsToolStripMenuItem_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(286, 24);
            toolStripMenuItem1.Text = "Close Fast File";
            toolStripMenuItem1.ToolTipText = "Save changes to the FF & closes the opened FF";
            toolStripMenuItem1.Click += closeFastFileToolStripMenuItem_Click;
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(286, 24);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.ToolTipText = "Save changes to the FF & exits";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // editToolStripMenuItem
            // 
            editToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { saveRawFileToolStripMenuItem, renameRawFileToolStripMenuItem, toolStripSeparator1, compressCodeToolStripMenuItem, removeCommentsToolStripMenuItem, checkSyntaxToolStripMenuItem });
            editToolStripMenuItem.Name = "editToolStripMenuItem";
            editToolStripMenuItem.Size = new Size(47, 24);
            editToolStripMenuItem.Text = "Edit";
            // 
            // saveRawFileToolStripMenuItem
            // 
            saveRawFileToolStripMenuItem.Enabled = false;
            saveRawFileToolStripMenuItem.Name = "saveRawFileToolStripMenuItem";
            saveRawFileToolStripMenuItem.Size = new Size(214, 24);
            saveRawFileToolStripMenuItem.Text = "Save Raw File (zone)";
            saveRawFileToolStripMenuItem.ToolTipText = "This will save the modified file extracted from the ff";
            saveRawFileToolStripMenuItem.Click += saveRawFileToolStripMenuItem_Click;
            // 
            // renameRawFileToolStripMenuItem
            // 
            renameRawFileToolStripMenuItem.Enabled = false;
            renameRawFileToolStripMenuItem.Name = "renameRawFileToolStripMenuItem";
            renameRawFileToolStripMenuItem.Size = new Size(214, 24);
            renameRawFileToolStripMenuItem.Text = "Rename Raw File";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(211, 6);
            // 
            // compressCodeToolStripMenuItem
            // 
            compressCodeToolStripMenuItem.Name = "compressCodeToolStripMenuItem";
            compressCodeToolStripMenuItem.Size = new Size(214, 24);
            compressCodeToolStripMenuItem.Text = "Compress Code";
            compressCodeToolStripMenuItem.Click += compressCodeToolStripMenuItem_Click;
            // 
            // removeCommentsToolStripMenuItem
            // 
            removeCommentsToolStripMenuItem.Name = "removeCommentsToolStripMenuItem";
            removeCommentsToolStripMenuItem.Size = new Size(214, 24);
            removeCommentsToolStripMenuItem.Text = "Remove Comments";
            removeCommentsToolStripMenuItem.Click += removeCommentsToolStripMenuItem_Click;
            // 
            // checkSyntaxToolStripMenuItem
            // 
            checkSyntaxToolStripMenuItem.Name = "checkSyntaxToolStripMenuItem";
            checkSyntaxToolStripMenuItem.Size = new Size(214, 24);
            checkSyntaxToolStripMenuItem.Text = "Check Syntax";
            checkSyntaxToolStripMenuItem.Click += checkSyntaxToolStripMenuItem_Click;
            // 
            // toolsToolStripMenuItem
            // 
            toolsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { injectFileToolStripMenuItem, rawFileToolsMenuItem, extractAllRawFilesToolStripMenuItem, toolStripMenuItem2 });
            toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            toolsToolStripMenuItem.Size = new Size(56, 24);
            toolsToolStripMenuItem.Text = "Tools";
            // 
            // injectFileToolStripMenuItem
            // 
            injectFileToolStripMenuItem.Name = "injectFileToolStripMenuItem";
            injectFileToolStripMenuItem.Size = new Size(210, 24);
            injectFileToolStripMenuItem.Text = "Inject Raw File";
            injectFileToolStripMenuItem.Click += injectFileToolStripMenuItem_Click;
            // 
            // rawFileToolsMenuItem
            // 
            rawFileToolsMenuItem.DropDownItems.AddRange(new ToolStripItem[] { increaseRawFileSizeToolStripMenuItem, searchRawFileTxtMenuItem });
            rawFileToolsMenuItem.Name = "rawFileToolsMenuItem";
            rawFileToolsMenuItem.Size = new Size(210, 24);
            rawFileToolsMenuItem.Text = "Raw File Tools";
            // 
            // increaseRawFileSizeToolStripMenuItem
            // 
            increaseRawFileSizeToolStripMenuItem.Name = "increaseRawFileSizeToolStripMenuItem";
            increaseRawFileSizeToolStripMenuItem.Size = new Size(222, 24);
            increaseRawFileSizeToolStripMenuItem.Text = "Increase Raw File Size";
            // 
            // searchRawFileTxtMenuItem
            // 
            searchRawFileTxtMenuItem.Name = "searchRawFileTxtMenuItem";
            searchRawFileTxtMenuItem.Size = new Size(222, 24);
            searchRawFileTxtMenuItem.Text = "Search Raw File Text";
            searchRawFileTxtMenuItem.Click += searchRawFileTxtMenuItem_Click;
            // 
            // extractAllRawFilesToolStripMenuItem
            // 
            extractAllRawFilesToolStripMenuItem.Name = "extractAllRawFilesToolStripMenuItem";
            extractAllRawFilesToolStripMenuItem.Size = new Size(210, 24);
            extractAllRawFilesToolStripMenuItem.Text = "Extract All Raw Files";
            extractAllRawFilesToolStripMenuItem.Click += extractAllRawFilesToolStripMenuItem_Click;
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.Size = new Size(210, 24);
            toolStripMenuItem2.Text = "Zone Hex View";
            toolStripMenuItem2.Click += toolStripMenuItem2_Click;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { aboutToolStripMenuItem, CheckForUpdatesToolStripMenuItem });
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(53, 24);
            helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(195, 24);
            aboutToolStripMenuItem.Text = "About";
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;
            // 
            // CheckForUpdatesToolStripMenuItem
            // 
            CheckForUpdatesToolStripMenuItem.Name = "CheckForUpdatesToolStripMenuItem";
            CheckForUpdatesToolStripMenuItem.Size = new Size(195, 24);
            CheckForUpdatesToolStripMenuItem.Text = "Check For Update";
            CheckForUpdatesToolStripMenuItem.Click += CheckForUpdatesToolStripMenuItem_Click;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(3, 3);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(filesTreeView);
            splitContainer1.Panel1.Padding = new Padding(8, 0, 0, 0);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(textEditorControlEx1);
            splitContainer1.Size = new Size(1436, 743);
            splitContainer1.SplitterDistance = 338;
            splitContainer1.TabIndex = 0;
            // 
            // textEditorControlEx1
            // 
            textEditorControlEx1.ContextMenuEnabled = true;
            textEditorControlEx1.Dock = DockStyle.Fill;
            textEditorControlEx1.FoldingStrategy = "C#";
            textEditorControlEx1.Font = new Font("Courier New", 10F);
            textEditorControlEx1.Location = new Point(0, 0);
            textEditorControlEx1.Name = "textEditorControlEx1";
            textEditorControlEx1.Size = new Size(1094, 743);
            textEditorControlEx1.SyntaxHighlighting = "C#";
            textEditorControlEx1.TabIndex = 0;
            textEditorControlEx1.TextChanged += textEditorControlEx1_TextChanged;
            // 
            // statusStripBottom
            // 
            statusStripBottom.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            statusStripBottom.Items.AddRange(new ToolStripItem[] { loadedFileNameStatusLabel, toolStripStatusLabel1, selectedItemStatusLabel, selectedFileMaxSizeStatusLabel, selectedFileCurrentSizeStatusLabel });
            statusStripBottom.Location = new Point(0, 805);
            statusStripBottom.Name = "statusStripBottom";
            statusStripBottom.Size = new Size(1450, 22);
            statusStripBottom.TabIndex = 2;
            // 
            // loadedFileNameStatusLabel
            // 
            loadedFileNameStatusLabel.Name = "loadedFileNameStatusLabel";
            loadedFileNameStatusLabel.Size = new Size(117, 21);
            loadedFileNameStatusLabel.Text = "File Name Here";
            loadedFileNameStatusLabel.Visible = false;
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(0, 17);
            // 
            // selectedItemStatusLabel
            // 
            selectedItemStatusLabel.Margin = new Padding(100, 3, 0, 2);
            selectedItemStatusLabel.Name = "selectedItemStatusLabel";
            selectedItemStatusLabel.Size = new Size(140, 21);
            selectedItemStatusLabel.Text = "Selected Item Here";
            selectedItemStatusLabel.Visible = false;
            // 
            // selectedFileMaxSizeStatusLabel
            // 
            selectedFileMaxSizeStatusLabel.Margin = new Padding(100, 3, 0, 2);
            selectedFileMaxSizeStatusLabel.Name = "selectedFileMaxSizeStatusLabel";
            selectedFileMaxSizeStatusLabel.Size = new Size(161, 21);
            selectedFileMaxSizeStatusLabel.Text = "Selected File Max Size";
            selectedFileMaxSizeStatusLabel.Visible = false;
            // 
            // selectedFileCurrentSizeStatusLabel
            // 
            selectedFileCurrentSizeStatusLabel.Margin = new Padding(50, 3, 0, 2);
            selectedFileCurrentSizeStatusLabel.Name = "selectedFileCurrentSizeStatusLabel";
            selectedFileCurrentSizeStatusLabel.Size = new Size(185, 21);
            selectedFileCurrentSizeStatusLabel.Text = "Selected File Current Size";
            selectedFileCurrentSizeStatusLabel.Visible = false;
            // 
            // filesTreeToolTip
            // 
            filesTreeToolTip.ShowAlways = true;
            filesTreeToolTip.ToolTipIcon = ToolTipIcon.Info;
            // 
            // mainTabControl
            // 
            mainTabControl.ContextMenuStrip = universalContextMenu;
            mainTabControl.Controls.Add(rawFilesPage);
            mainTabControl.Controls.Add(stringTablesTabPage);
            mainTabControl.Controls.Add(collision_Map_AssetTabPage);
            mainTabControl.Controls.Add(localizeTabPage);
            mainTabControl.Controls.Add(tagsTabPage);
            mainTabControl.Controls.Add(assetPoolTabPage);
            mainTabControl.Controls.Add(zoneFileTabPage);
            mainTabControl.Dock = DockStyle.Fill;
            mainTabControl.Location = new Point(0, 28);
            mainTabControl.Name = "mainTabControl";
            mainTabControl.SelectedIndex = 0;
            mainTabControl.Size = new Size(1450, 777);
            mainTabControl.TabIndex = 3;
            // 
            // universalContextMenu
            // 
            universalContextMenu.Items.AddRange(new ToolStripItem[] { copyToolStripMenuItem });
            universalContextMenu.Name = "contextMenuStripTagsCopy";
            universalContextMenu.Size = new Size(103, 26);
            // 
            // copyToolStripMenuItem
            // 
            copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            copyToolStripMenuItem.Size = new Size(102, 22);
            copyToolStripMenuItem.Text = "Copy";
            copyToolStripMenuItem.Click += copyToolStripMenuItem_Click;
            // 
            // rawFilesPage
            // 
            rawFilesPage.Controls.Add(splitContainer1);
            rawFilesPage.Location = new Point(4, 24);
            rawFilesPage.Name = "rawFilesPage";
            rawFilesPage.Padding = new Padding(3);
            rawFilesPage.Size = new Size(1442, 749);
            rawFilesPage.TabIndex = 0;
            rawFilesPage.Text = "Raw Files";
            rawFilesPage.UseVisualStyleBackColor = true;
            // 
            // stringTablesTabPage
            // 
            stringTablesTabPage.Controls.Add(stringTableListView);
            stringTablesTabPage.Controls.Add(stringTableTreeView);
            stringTablesTabPage.Location = new Point(4, 24);
            stringTablesTabPage.Name = "stringTablesTabPage";
            stringTablesTabPage.Padding = new Padding(3);
            stringTablesTabPage.Size = new Size(1442, 749);
            stringTablesTabPage.TabIndex = 3;
            stringTablesTabPage.Text = "String Tables";
            stringTablesTabPage.UseVisualStyleBackColor = true;
            // 
            // stringTableListView
            // 
            stringTableListView.Location = new Point(247, 0);
            stringTableListView.Name = "stringTableListView";
            stringTableListView.Size = new Size(384, 749);
            stringTableListView.TabIndex = 1;
            stringTableListView.UseCompatibleStateImageBehavior = false;
            // 
            // stringTableTreeView
            // 
            stringTableTreeView.ContextMenuStrip = universalContextMenu;
            stringTableTreeView.Location = new Point(0, 0);
            stringTableTreeView.Name = "stringTableTreeView";
            stringTableTreeView.Size = new Size(250, 749);
            stringTableTreeView.TabIndex = 0;
            stringTableTreeView.BeforeSelect += stringTableTreeView_BeforeSelect;
            stringTableTreeView.AfterSelect += stringTableTreeView_AfterSelect;
            stringTableTreeView.MouseDown += treeView_MouseDownCopy;
            // 
            // collision_Map_AssetTabPage
            // 
            collision_Map_AssetTabPage.Controls.Add(treeViewMapEnt);
            collision_Map_AssetTabPage.Location = new Point(4, 24);
            collision_Map_AssetTabPage.Name = "collision_Map_AssetTabPage";
            collision_Map_AssetTabPage.Padding = new Padding(3);
            collision_Map_AssetTabPage.Size = new Size(1442, 749);
            collision_Map_AssetTabPage.TabIndex = 4;
            collision_Map_AssetTabPage.Text = "Collision Map Data";
            collision_Map_AssetTabPage.UseVisualStyleBackColor = true;
            // 
            // treeViewMapEnt
            // 
            treeViewMapEnt.ContextMenuStrip = universalContextMenu;
            treeViewMapEnt.Location = new Point(0, 0);
            treeViewMapEnt.Name = "treeViewMapEnt";
            treeViewMapEnt.Size = new Size(307, 749);
            treeViewMapEnt.TabIndex = 2;
            treeViewMapEnt.MouseDown += treeView_MouseDownCopy;
            // 
            // localizeTabPage
            // 
            localizeTabPage.Controls.Add(localizeListView);
            localizeTabPage.Location = new Point(4, 24);
            localizeTabPage.Name = "localizeTabPage";
            localizeTabPage.Padding = new Padding(3);
            localizeTabPage.Size = new Size(1442, 749);
            localizeTabPage.TabIndex = 6;
            localizeTabPage.Text = "Localize";
            localizeTabPage.UseVisualStyleBackColor = true;
            // 
            // localizeListView
            // 
            localizeListView.ContextMenuStrip = universalContextMenu;
            localizeListView.Dock = DockStyle.Fill;
            localizeListView.Location = new Point(3, 3);
            localizeListView.Name = "localizeListView";
            localizeListView.Size = new Size(1436, 743);
            localizeListView.TabIndex = 0;
            localizeListView.UseCompatibleStateImageBehavior = false;
            localizeListView.MouseDown += listView_MouseDownCopy;
            // 
            // tagsTabPage
            // 
            tagsTabPage.Controls.Add(tagsListView);
            tagsTabPage.Location = new Point(4, 24);
            tagsTabPage.Name = "tagsTabPage";
            tagsTabPage.Padding = new Padding(3);
            tagsTabPage.Size = new Size(1442, 749);
            tagsTabPage.TabIndex = 2;
            tagsTabPage.Text = "Tags";
            tagsTabPage.UseVisualStyleBackColor = true;
            // 
            // tagsListView
            // 
            tagsListView.ContextMenuStrip = universalContextMenu;
            tagsListView.Location = new Point(0, 0);
            tagsListView.Name = "tagsListView";
            tagsListView.Size = new Size(487, 750);
            tagsListView.TabIndex = 0;
            tagsListView.UseCompatibleStateImageBehavior = false;
            tagsListView.View = View.Details;
            tagsListView.MouseDown += listView_MouseDownCopy;
            // 
            // assetPoolTabPage
            // 
            assetPoolTabPage.Controls.Add(assetPoolListView);
            assetPoolTabPage.Location = new Point(4, 24);
            assetPoolTabPage.Name = "assetPoolTabPage";
            assetPoolTabPage.Padding = new Padding(3);
            assetPoolTabPage.Size = new Size(1442, 749);
            assetPoolTabPage.TabIndex = 5;
            assetPoolTabPage.Text = "Asset Pool";
            assetPoolTabPage.UseVisualStyleBackColor = true;
            // 
            // assetPoolListView
            // 
            assetPoolListView.ContextMenuStrip = universalContextMenu;
            assetPoolListView.Dock = DockStyle.Fill;
            assetPoolListView.Location = new Point(3, 3);
            assetPoolListView.Name = "assetPoolListView";
            assetPoolListView.Size = new Size(1436, 743);
            assetPoolListView.TabIndex = 0;
            assetPoolListView.UseCompatibleStateImageBehavior = false;
            assetPoolListView.MouseDown += listView_MouseDownCopy;
            // 
            // zoneFileTabPage
            // 
            zoneFileTabPage.Controls.Add(zoneInfoDataGridView);
            zoneFileTabPage.Location = new Point(4, 24);
            zoneFileTabPage.Name = "zoneFileTabPage";
            zoneFileTabPage.Padding = new Padding(3);
            zoneFileTabPage.Size = new Size(1442, 749);
            zoneFileTabPage.TabIndex = 1;
            zoneFileTabPage.Text = "Zone Header";
            zoneFileTabPage.UseVisualStyleBackColor = true;
            // 
            // zoneInfoDataGridView
            // 
            zoneInfoDataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            zoneInfoDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            zoneInfoDataGridView.Location = new Point(-4, 0);
            zoneInfoDataGridView.Name = "zoneInfoDataGridView";
            zoneInfoDataGridView.Size = new Size(493, 432);
            zoneInfoDataGridView.TabIndex = 0;
            zoneInfoDataGridView.MouseDown += dataGrid_MouseDownCopy;
            // 
            // MainWindowForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1450, 827);
            Controls.Add(mainTabControl);
            Controls.Add(statusStripBottom);
            Controls.Add(menuStripTopToolbar);
            MainMenuStrip = menuStripTopToolbar;
            Name = "MainWindowForm";
            contextMenuStripRawFiles.ResumeLayout(false);
            menuStripTopToolbar.ResumeLayout(false);
            menuStripTopToolbar.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            statusStripBottom.ResumeLayout(false);
            statusStripBottom.PerformLayout();
            mainTabControl.ResumeLayout(false);
            universalContextMenu.ResumeLayout(false);
            rawFilesPage.ResumeLayout(false);
            stringTablesTabPage.ResumeLayout(false);
            collision_Map_AssetTabPage.ResumeLayout(false);
            localizeTabPage.ResumeLayout(false);
            tagsTabPage.ResumeLayout(false);
            assetPoolTabPage.ResumeLayout(false);
            zoneFileTabPage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)zoneInfoDataGridView).EndInit();
            ((System.ComponentModel.ISupportInitialize)bindingSource1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TreeView filesTreeView;
        private MenuStrip menuStripTopToolbar;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem openFastFileToolStripMenuItem;
        private SplitContainer splitContainer1;
        private StatusStrip statusStripBottom;
        private ToolStripStatusLabel loadedFileNameStatusLabel;
        private ToolStripStatusLabel selectedItemStatusLabel;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private ToolStripStatusLabel selectedFileMaxSizeStatusLabel;
        private ToolStripStatusLabel selectedFileCurrentSizeStatusLabel;
        private TextEditorControlEx textEditorControlEx1;
        private ToolStripMenuItem saveFastFileToolStripMenuItem;
        private ToolStripMenuItem saveFastFileAsToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem editToolStripMenuItem;
        private ToolStripMenuItem toolsToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem saveRawFileToolStripMenuItem;
        private ToolStripMenuItem renameRawFileToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem compressCodeToolStripMenuItem;
        private ToolStripMenuItem removeCommentsToolStripMenuItem;
        private ToolStripMenuItem checkSyntaxToolStripMenuItem;
        private ToolStripMenuItem searchRawFileTxtMenuItem;
        private ToolStripMenuItem injectFileToolStripMenuItem;
        private ContextMenuStrip contextMenuStripRawFiles;
        private ToolStripMenuItem exportFileMenuItem;
        private ToolStripMenuItem toolStripMenuItem1;
        private ToolTip filesTreeToolTip;
        private ToolStripMenuItem renameFileToolStripMenuItem;
        private TabControl mainTabControl;
        private TabPage rawFilesPage;
        private TabPage zoneFileTabPage;
        private DataGridView zoneInfoDataGridView;
        private BindingSource bindingSource1;
        private TabPage tagsTabPage;
        private TabPage stringTablesTabPage;
        private TreeView stringTableTreeView;
        private ListView tagsListView;
        private ContextMenuStrip universalContextMenu;
        private ToolStripMenuItem copyToolStripMenuItem;
        private TabPage collision_Map_AssetTabPage;
        private TreeView treeViewMapEnt;
        private ToolStripMenuItem rawFileToolsMenuItem;
        private ToolStripMenuItem increaseRawFileSizeToolStripMenuItem;
        private TabPage assetPoolTabPage;
        private ListView assetPoolListView;
        private ListView stringTableListView;
        private TabPage localizeTabPage;
        private ListView localizeListView;
        private ToolStripMenuItem extractAllRawFilesToolStripMenuItem;
        private ToolStripMenuItem increaseFileSizeToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStripMenuItem CheckForUpdatesToolStripMenuItem;
        private ToolStripMenuItem toolStripMenuItem2;
        private ToolStripMenuItem COD5ToolStripMenuItem;
        private ToolStripMenuItem cOD4ToolStripMenuItem;
    }
}