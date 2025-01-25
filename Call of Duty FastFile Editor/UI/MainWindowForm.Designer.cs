using ICSharpCode.TextEditor;

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
            textEditorControl1 = new TextEditorControl();
            filesTreeView = new TreeView();
            contextMenuStripRawFiles = new ContextMenuStrip(components);
            exportFileMenuItem = new ToolStripMenuItem();
            renameFileToolStripMenuItem = new ToolStripMenuItem();
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            openFastFileToolStripMenuItem = new ToolStripMenuItem();
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
            fileStructureInfoToolStripMenuItem = new ToolStripMenuItem();
            saveFileToPCToolStripMenuItem = new ToolStripMenuItem();
            originalFastFilesToolStripMenuItem = new ToolStripMenuItem();
            worldAtWarToolStripMenuItem = new ToolStripMenuItem();
            defaultffToolStripMenuItem = new ToolStripMenuItem();
            patchmpffToolStripMenuItem1 = new ToolStripMenuItem();
            patchffNachtDerUntotenToolStripMenuItem = new ToolStripMenuItem();
            patchffZombieVerruckToolStripMenuItem = new ToolStripMenuItem();
            nazizombiesumpfpatchffToolStripMenuItem = new ToolStripMenuItem();
            nazizombiefactorypatchffToolStripMenuItem = new ToolStripMenuItem();
            modernWarfareToolStripMenuItem = new ToolStripMenuItem();
            patchmpffToolStripMenuItem = new ToolStripMenuItem();
            checkForUpdateToolStripMenuItem = new ToolStripMenuItem();
            injectFileToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            splitContainer1 = new SplitContainer();
            statusStrip1 = new StatusStrip();
            loadedFileNameStatusLabel = new ToolStripStatusLabel();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            selectedItemStatusLabel = new ToolStripStatusLabel();
            selectedFileMaxSizeStatusLabel = new ToolStripStatusLabel();
            selectedFileCurrentSizeStatusLabel = new ToolStripStatusLabel();
            filesTreeToolTip = new ToolTip(components);
            tabControl1 = new TabControl();
            rawFilesPage = new TabPage();
            tabPage2 = new TabPage();
            dataGridView1 = new DataGridView();
            tabPage1 = new TabPage();
            tagsListView = new ListView();
            contextMenuStripTagsCopy = new ContextMenuStrip(components);
            copyToolStripMenuItem = new ToolStripMenuItem();
            tabPage3 = new TabPage();
            stringTablesTreeView = new TreeView();
            bindingSource1 = new BindingSource(components);
            contextMenuStripRawFiles.SuspendLayout();
            menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            statusStrip1.SuspendLayout();
            tabControl1.SuspendLayout();
            rawFilesPage.SuspendLayout();
            tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            tabPage1.SuspendLayout();
            contextMenuStripTagsCopy.SuspendLayout();
            tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)bindingSource1).BeginInit();
            SuspendLayout();
            // 
            // textEditorControl1
            // 
            textEditorControl1.Dock = DockStyle.Fill;
            textEditorControl1.IsReadOnly = false;
            textEditorControl1.Location = new Point(0, 0);
            textEditorControl1.Name = "textEditorControl1";
            textEditorControl1.Size = new Size(1094, 743);
            textEditorControl1.TabIndex = 0;
            textEditorControl1.TextChanged += textEditorControl1_TextChanged;
            // 
            // filesTreeView
            // 
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
            contextMenuStripRawFiles.Items.AddRange(new ToolStripItem[] { exportFileMenuItem, renameFileToolStripMenuItem });
            contextMenuStripRawFiles.Name = "contextMenuStrip1";
            contextMenuStripRawFiles.Size = new Size(139, 48);
            // 
            // exportFileMenuItem
            // 
            exportFileMenuItem.Name = "exportFileMenuItem";
            exportFileMenuItem.Size = new Size(138, 22);
            exportFileMenuItem.Text = "Export File";
            exportFileMenuItem.ToolTipText = "Export the selected raw file";
            exportFileMenuItem.Click += exportFileMenuItem_Click;
            // 
            // renameFileToolStripMenuItem
            // 
            renameFileToolStripMenuItem.Name = "renameFileToolStripMenuItem";
            renameFileToolStripMenuItem.Size = new Size(138, 22);
            renameFileToolStripMenuItem.Text = "Rename File";
            renameFileToolStripMenuItem.Click += renameFileToolStripMenuItem_Click;
            // 
            // menuStrip1
            // 
            menuStrip1.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, editToolStripMenuItem, toolsToolStripMenuItem, aboutToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1450, 28);
            menuStrip1.TabIndex = 1;
            menuStrip1.Text = "menuStrip1";
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
            openFastFileToolStripMenuItem.Name = "openFastFileToolStripMenuItem";
            openFastFileToolStripMenuItem.Size = new Size(286, 24);
            openFastFileToolStripMenuItem.Text = "Open Fast File";
            openFastFileToolStripMenuItem.Click += openFastFileToolStripMenuItem_Click;
            // 
            // saveFastFileToolStripMenuItem
            // 
            saveFastFileToolStripMenuItem.Enabled = false;
            saveFastFileToolStripMenuItem.Name = "saveFastFileToolStripMenuItem";
            saveFastFileToolStripMenuItem.Size = new Size(286, 24);
            saveFastFileToolStripMenuItem.Text = "Save Fast File (Recompress)";
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
            toolStripMenuItem1.Click += toolStripMenuItem1_Click;
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(286, 24);
            exitToolStripMenuItem.Text = "Exit";
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
            toolsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { fileStructureInfoToolStripMenuItem, saveFileToPCToolStripMenuItem, originalFastFilesToolStripMenuItem, checkForUpdateToolStripMenuItem, injectFileToolStripMenuItem });
            toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            toolsToolStripMenuItem.Size = new Size(56, 24);
            toolsToolStripMenuItem.Text = "Tools";
            // 
            // fileStructureInfoToolStripMenuItem
            // 
            fileStructureInfoToolStripMenuItem.Name = "fileStructureInfoToolStripMenuItem";
            fileStructureInfoToolStripMenuItem.Size = new Size(195, 24);
            fileStructureInfoToolStripMenuItem.Text = "File Structure Info";
            fileStructureInfoToolStripMenuItem.Click += fileStructureInfoToolStripMenuItem_Click;
            // 
            // saveFileToPCToolStripMenuItem
            // 
            saveFileToPCToolStripMenuItem.Name = "saveFileToPCToolStripMenuItem";
            saveFileToPCToolStripMenuItem.Size = new Size(195, 24);
            saveFileToPCToolStripMenuItem.Text = "Save File To PC";
            saveFileToPCToolStripMenuItem.Click += saveFileToPCToolStripMenuItem_Click;
            // 
            // originalFastFilesToolStripMenuItem
            // 
            originalFastFilesToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { worldAtWarToolStripMenuItem, modernWarfareToolStripMenuItem });
            originalFastFilesToolStripMenuItem.Name = "originalFastFilesToolStripMenuItem";
            originalFastFilesToolStripMenuItem.Size = new Size(195, 24);
            originalFastFilesToolStripMenuItem.Text = "Original Fast Files";
            // 
            // worldAtWarToolStripMenuItem
            // 
            worldAtWarToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { defaultffToolStripMenuItem, patchmpffToolStripMenuItem1, patchffNachtDerUntotenToolStripMenuItem, patchffZombieVerruckToolStripMenuItem, nazizombiesumpfpatchffToolStripMenuItem, nazizombiefactorypatchffToolStripMenuItem });
            worldAtWarToolStripMenuItem.Name = "worldAtWarToolStripMenuItem";
            worldAtWarToolStripMenuItem.Size = new Size(186, 24);
            worldAtWarToolStripMenuItem.Text = "World at War";
            // 
            // defaultffToolStripMenuItem
            // 
            defaultffToolStripMenuItem.Name = "defaultffToolStripMenuItem";
            defaultffToolStripMenuItem.Size = new Size(269, 24);
            defaultffToolStripMenuItem.Text = "default.ff";
            defaultffToolStripMenuItem.Click += defaultffToolStripMenuItem_Click;
            // 
            // patchmpffToolStripMenuItem1
            // 
            patchmpffToolStripMenuItem1.Name = "patchmpffToolStripMenuItem1";
            patchmpffToolStripMenuItem1.Size = new Size(269, 24);
            patchmpffToolStripMenuItem1.Text = "patch_mp.ff";
            patchmpffToolStripMenuItem1.Click += patchmpffToolStripMenuItem1_Click;
            // 
            // patchffNachtDerUntotenToolStripMenuItem
            // 
            patchffNachtDerUntotenToolStripMenuItem.Enabled = false;
            patchffNachtDerUntotenToolStripMenuItem.Name = "patchffNachtDerUntotenToolStripMenuItem";
            patchffNachtDerUntotenToolStripMenuItem.Size = new Size(269, 24);
            patchffNachtDerUntotenToolStripMenuItem.Text = "patch.ff - Nacht Der Untoten";
            // 
            // patchffZombieVerruckToolStripMenuItem
            // 
            patchffZombieVerruckToolStripMenuItem.Enabled = false;
            patchffZombieVerruckToolStripMenuItem.Name = "patchffZombieVerruckToolStripMenuItem";
            patchffZombieVerruckToolStripMenuItem.Size = new Size(269, 24);
            patchffZombieVerruckToolStripMenuItem.Text = "patch.ff - Zombie Verrückt";
            // 
            // nazizombiesumpfpatchffToolStripMenuItem
            // 
            nazizombiesumpfpatchffToolStripMenuItem.Enabled = false;
            nazizombiesumpfpatchffToolStripMenuItem.Name = "nazizombiesumpfpatchffToolStripMenuItem";
            nazizombiesumpfpatchffToolStripMenuItem.Size = new Size(269, 24);
            nazizombiesumpfpatchffToolStripMenuItem.Text = "nazi_zombie_sumpf_patch.ff";
            // 
            // nazizombiefactorypatchffToolStripMenuItem
            // 
            nazizombiefactorypatchffToolStripMenuItem.Name = "nazizombiefactorypatchffToolStripMenuItem";
            nazizombiefactorypatchffToolStripMenuItem.Size = new Size(269, 24);
            nazizombiefactorypatchffToolStripMenuItem.Text = "nazi_zombie_factory_patch.ff";
            nazizombiefactorypatchffToolStripMenuItem.Click += nazizombiefactorypatchffToolStripMenuItem_Click;
            // 
            // modernWarfareToolStripMenuItem
            // 
            modernWarfareToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { patchmpffToolStripMenuItem });
            modernWarfareToolStripMenuItem.Name = "modernWarfareToolStripMenuItem";
            modernWarfareToolStripMenuItem.Size = new Size(186, 24);
            modernWarfareToolStripMenuItem.Text = "Modern Warfare";
            // 
            // patchmpffToolStripMenuItem
            // 
            patchmpffToolStripMenuItem.Name = "patchmpffToolStripMenuItem";
            patchmpffToolStripMenuItem.Size = new Size(156, 24);
            patchmpffToolStripMenuItem.Text = "patch_mp.ff";
            patchmpffToolStripMenuItem.Click += patchmpffToolStripMenuItem_Click;
            // 
            // checkForUpdateToolStripMenuItem
            // 
            checkForUpdateToolStripMenuItem.Name = "checkForUpdateToolStripMenuItem";
            checkForUpdateToolStripMenuItem.Size = new Size(195, 24);
            checkForUpdateToolStripMenuItem.Text = "Check For Update";
            checkForUpdateToolStripMenuItem.Click += checkForUpdateToolStripMenuItem_Click;
            // 
            // injectFileToolStripMenuItem
            // 
            injectFileToolStripMenuItem.Name = "injectFileToolStripMenuItem";
            injectFileToolStripMenuItem.Size = new Size(195, 24);
            injectFileToolStripMenuItem.Text = "Inject File";
            injectFileToolStripMenuItem.Click += injectFileToolStripMenuItem_Click;
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(62, 24);
            aboutToolStripMenuItem.Text = "About";
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;
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
            splitContainer1.Panel2.Controls.Add(textEditorControl1);
            splitContainer1.Size = new Size(1436, 743);
            splitContainer1.SplitterDistance = 338;
            splitContainer1.TabIndex = 0;
            // 
            // statusStrip1
            // 
            statusStrip1.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            statusStrip1.Items.AddRange(new ToolStripItem[] { loadedFileNameStatusLabel, toolStripStatusLabel1, selectedItemStatusLabel, selectedFileMaxSizeStatusLabel, selectedFileCurrentSizeStatusLabel });
            statusStrip1.Location = new Point(0, 805);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(1450, 22);
            statusStrip1.TabIndex = 2;
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
            // tabControl1
            // 
            tabControl1.Controls.Add(rawFilesPage);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage3);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 28);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(1450, 777);
            tabControl1.TabIndex = 3;
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
            // tabPage2
            // 
            tabPage2.Controls.Add(dataGridView1);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(1442, 749);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Raw File Info";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // dataGridView1
            // 
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new Point(-4, 0);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.Size = new Size(493, 432);
            dataGridView1.TabIndex = 0;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(tagsListView);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(1442, 749);
            tabPage1.TabIndex = 2;
            tabPage1.Text = "Tags";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // tagsListView
            // 
            tagsListView.ContextMenuStrip = contextMenuStripTagsCopy;
            tagsListView.Location = new Point(0, 0);
            tagsListView.Name = "tagsListView";
            tagsListView.Size = new Size(487, 750);
            tagsListView.Sorting = SortOrder.Ascending;
            tagsListView.TabIndex = 0;
            tagsListView.UseCompatibleStateImageBehavior = false;
            tagsListView.View = View.Details;
            // 
            // contextMenuStripTagsCopy
            // 
            contextMenuStripTagsCopy.Items.AddRange(new ToolStripItem[] { copyToolStripMenuItem });
            contextMenuStripTagsCopy.Name = "contextMenuStripTagsCopy";
            contextMenuStripTagsCopy.Size = new Size(103, 26);
            // 
            // copyToolStripMenuItem
            // 
            copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            copyToolStripMenuItem.Size = new Size(102, 22);
            copyToolStripMenuItem.Text = "Copy";
            copyToolStripMenuItem.Click += copyToolStripMenuItem_Click;
            // 
            // tabPage3
            // 
            tabPage3.Controls.Add(stringTablesTreeView);
            tabPage3.Location = new Point(4, 24);
            tabPage3.Name = "tabPage3";
            tabPage3.Padding = new Padding(3);
            tabPage3.Size = new Size(1442, 749);
            tabPage3.TabIndex = 3;
            tabPage3.Text = "String Tables";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // stringTablesTreeView
            // 
            stringTablesTreeView.Location = new Point(0, 0);
            stringTablesTreeView.Name = "stringTablesTreeView";
            stringTablesTreeView.Size = new Size(250, 749);
            stringTablesTreeView.TabIndex = 0;
            // 
            // MainWindowForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1450, 827);
            Controls.Add(tabControl1);
            Controls.Add(statusStrip1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "MainWindowForm";
            FormClosed += Form1_FormClosed;
            contextMenuStripRawFiles.ResumeLayout(false);
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            tabControl1.ResumeLayout(false);
            rawFilesPage.ResumeLayout(false);
            tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            tabPage1.ResumeLayout(false);
            contextMenuStripTagsCopy.ResumeLayout(false);
            tabPage3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)bindingSource1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TreeView filesTreeView;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem openFastFileToolStripMenuItem;
        private SplitContainer splitContainer1;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel loadedFileNameStatusLabel;
        private ToolStripStatusLabel selectedItemStatusLabel;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private ToolStripStatusLabel selectedFileMaxSizeStatusLabel;
        private ToolStripStatusLabel selectedFileCurrentSizeStatusLabel;
        private TextEditorControl textEditorControl1;
        private ToolStripMenuItem saveFastFileToolStripMenuItem;
        private ToolStripMenuItem saveFastFileAsToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem editToolStripMenuItem;
        private ToolStripMenuItem toolsToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStripMenuItem saveRawFileToolStripMenuItem;
        private ToolStripMenuItem renameRawFileToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem compressCodeToolStripMenuItem;
        private ToolStripMenuItem removeCommentsToolStripMenuItem;
        private ToolStripMenuItem checkSyntaxToolStripMenuItem;
        private ToolStripMenuItem saveFileToPCToolStripMenuItem;
        private ToolStripMenuItem fileStructureInfoToolStripMenuItem;
        private ToolStripMenuItem originalFastFilesToolStripMenuItem;
        private ToolStripMenuItem modernWarfareToolStripMenuItem;
        private ToolStripMenuItem patchmpffToolStripMenuItem;
        private ToolStripMenuItem worldAtWarToolStripMenuItem;
        private ToolStripMenuItem defaultffToolStripMenuItem;
        private ToolStripMenuItem patchmpffToolStripMenuItem1;
        private ToolStripMenuItem patchffNachtDerUntotenToolStripMenuItem;
        private ToolStripMenuItem patchffZombieVerruckToolStripMenuItem;
        private ToolStripMenuItem nazizombiesumpfpatchffToolStripMenuItem;
        private ToolStripMenuItem nazizombiefactorypatchffToolStripMenuItem;
        private ToolStripMenuItem checkForUpdateToolStripMenuItem;
        private ToolStripMenuItem injectFileToolStripMenuItem;
        private ContextMenuStrip contextMenuStripRawFiles;
        private ToolStripMenuItem exportFileMenuItem;
        private ToolStripMenuItem toolStripMenuItem1;
        private ToolTip filesTreeToolTip;
        private ToolStripMenuItem renameFileToolStripMenuItem;
        private TabControl tabControl1;
        private TabPage rawFilesPage;
        private TabPage tabPage2;
        private DataGridView dataGridView1;
        private BindingSource bindingSource1;
        private TabPage tabPage1;
        private TabPage tabPage3;
        private TreeView stringTablesTreeView;
        private ListView tagsListView;
        private ContextMenuStrip contextMenuStripTagsCopy;
        private ToolStripMenuItem copyToolStripMenuItem;
    }
}