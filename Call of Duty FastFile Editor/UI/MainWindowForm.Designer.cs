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
            textEditorControl1 = new TextEditorControl();
            filesTreeView = new TreeView();
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            openFastFileToolStripMenuItem = new ToolStripMenuItem();
            saveFastFileToolStripMenuItem = new ToolStripMenuItem();
            saveFastFileAsToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem = new ToolStripMenuItem();
            editToolStripMenuItem = new ToolStripMenuItem();
            undoToolStripMenuItem = new ToolStripMenuItem();
            redoToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            saveRawFileToolStripMenuItem = new ToolStripMenuItem();
            renameRawFileToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            beautifyCodeToolStripMenuItem = new ToolStripMenuItem();
            compressCodeToolStripMenuItem = new ToolStripMenuItem();
            removeCommentsToolStripMenuItem = new ToolStripMenuItem();
            checkSyntaxToolStripMenuItem = new ToolStripMenuItem();
            toolsToolStripMenuItem = new ToolStripMenuItem();
            saveFileToPCToolStripMenuItem = new ToolStripMenuItem();
            fileStructureInfoToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            splitContainer1 = new SplitContainer();
            statusStrip1 = new StatusStrip();
            loadedFileNameStatusLabel = new ToolStripStatusLabel();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            selectedItemStatusLabel = new ToolStripStatusLabel();
            selectedFileMaxSizeStatusLabel = new ToolStripStatusLabel();
            selectedFileCurrentSizeStatusLabel = new ToolStripStatusLabel();
            menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // textEditorControl1
            // 
            textEditorControl1.Dock = DockStyle.Fill;
            textEditorControl1.IsReadOnly = false;
            textEditorControl1.Location = new Point(0, 0);
            textEditorControl1.Name = "textEditorControl1";
            textEditorControl1.Size = new Size(1104, 777);
            textEditorControl1.TabIndex = 0;
            textEditorControl1.TextChanged += textEditorControl1_TextChanged;
            // 
            // filesTreeView
            // 
            filesTreeView.Dock = DockStyle.Fill;
            filesTreeView.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            filesTreeView.HideSelection = false;
            filesTreeView.Location = new Point(0, 0);
            filesTreeView.Name = "filesTreeView";
            filesTreeView.Size = new Size(342, 777);
            filesTreeView.TabIndex = 0;
            filesTreeView.AfterSelect += filesTreeView_AfterSelect;
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
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openFastFileToolStripMenuItem, saveFastFileToolStripMenuItem, saveFastFileAsToolStripMenuItem, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(44, 24);
            fileToolStripMenuItem.Text = "File";
            // 
            // openFastFileToolStripMenuItem
            // 
            openFastFileToolStripMenuItem.Name = "openFastFileToolStripMenuItem";
            openFastFileToolStripMenuItem.Size = new Size(192, 24);
            openFastFileToolStripMenuItem.Text = "Open Fast File";
            openFastFileToolStripMenuItem.Click += openFastFileToolStripMenuItem_Click;
            // 
            // saveFastFileToolStripMenuItem
            // 
            saveFastFileToolStripMenuItem.Name = "saveFastFileToolStripMenuItem";
            saveFastFileToolStripMenuItem.Size = new Size(192, 24);
            saveFastFileToolStripMenuItem.Text = "Save Fast File";
            saveFastFileToolStripMenuItem.Click += saveFastFileToolStripMenuItem_Click;
            // 
            // saveFastFileAsToolStripMenuItem
            // 
            saveFastFileAsToolStripMenuItem.Name = "saveFastFileAsToolStripMenuItem";
            saveFastFileAsToolStripMenuItem.Size = new Size(192, 24);
            saveFastFileAsToolStripMenuItem.Text = "Save Fast File as...";
            saveFastFileAsToolStripMenuItem.Click += saveFastFileAsToolStripMenuItem_Click;
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(192, 24);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // editToolStripMenuItem
            // 
            editToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { undoToolStripMenuItem, redoToolStripMenuItem, toolStripSeparator2, saveRawFileToolStripMenuItem, renameRawFileToolStripMenuItem, toolStripSeparator1, beautifyCodeToolStripMenuItem, compressCodeToolStripMenuItem, removeCommentsToolStripMenuItem, checkSyntaxToolStripMenuItem });
            editToolStripMenuItem.Name = "editToolStripMenuItem";
            editToolStripMenuItem.Size = new Size(47, 24);
            editToolStripMenuItem.Text = "Edit";
            // 
            // undoToolStripMenuItem
            // 
            undoToolStripMenuItem.Enabled = false;
            undoToolStripMenuItem.Name = "undoToolStripMenuItem";
            undoToolStripMenuItem.Size = new Size(207, 24);
            undoToolStripMenuItem.Text = "Undo";
            undoToolStripMenuItem.Click += undoToolStripMenuItem_Click;
            // 
            // redoToolStripMenuItem
            // 
            redoToolStripMenuItem.Enabled = false;
            redoToolStripMenuItem.Name = "redoToolStripMenuItem";
            redoToolStripMenuItem.Size = new Size(207, 24);
            redoToolStripMenuItem.Text = "Redo";
            redoToolStripMenuItem.Click += redoToolStripMenuItem_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(204, 6);
            // 
            // saveRawFileToolStripMenuItem
            // 
            saveRawFileToolStripMenuItem.Enabled = false;
            saveRawFileToolStripMenuItem.Name = "saveRawFileToolStripMenuItem";
            saveRawFileToolStripMenuItem.Size = new Size(207, 24);
            saveRawFileToolStripMenuItem.Text = "Save Raw File";
            saveRawFileToolStripMenuItem.ToolTipText = "This will save the modified file extracted from the ff";
            saveRawFileToolStripMenuItem.Click += saveRawFileToolStripMenuItem_Click;
            // 
            // renameRawFileToolStripMenuItem
            // 
            renameRawFileToolStripMenuItem.Enabled = false;
            renameRawFileToolStripMenuItem.Name = "renameRawFileToolStripMenuItem";
            renameRawFileToolStripMenuItem.Size = new Size(207, 24);
            renameRawFileToolStripMenuItem.Text = "Rename Raw File";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(204, 6);
            // 
            // beautifyCodeToolStripMenuItem
            // 
            beautifyCodeToolStripMenuItem.Name = "beautifyCodeToolStripMenuItem";
            beautifyCodeToolStripMenuItem.Size = new Size(207, 24);
            beautifyCodeToolStripMenuItem.Text = "Beautify Code";
            // 
            // compressCodeToolStripMenuItem
            // 
            compressCodeToolStripMenuItem.Name = "compressCodeToolStripMenuItem";
            compressCodeToolStripMenuItem.Size = new Size(207, 24);
            compressCodeToolStripMenuItem.Text = "Compress Code";
            // 
            // removeCommentsToolStripMenuItem
            // 
            removeCommentsToolStripMenuItem.Name = "removeCommentsToolStripMenuItem";
            removeCommentsToolStripMenuItem.Size = new Size(207, 24);
            removeCommentsToolStripMenuItem.Text = "Remove Comments";
            removeCommentsToolStripMenuItem.Click += removeCommentsToolStripMenuItem_Click;
            // 
            // checkSyntaxToolStripMenuItem
            // 
            checkSyntaxToolStripMenuItem.Name = "checkSyntaxToolStripMenuItem";
            checkSyntaxToolStripMenuItem.Size = new Size(207, 24);
            checkSyntaxToolStripMenuItem.Text = "Check Syntax";
            // 
            // toolsToolStripMenuItem
            // 
            toolsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { fileStructureInfoToolStripMenuItem, saveFileToPCToolStripMenuItem });
            toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            toolsToolStripMenuItem.Size = new Size(56, 24);
            toolsToolStripMenuItem.Text = "Tools";
            // 
            // saveFileToPCToolStripMenuItem
            // 
            saveFileToPCToolStripMenuItem.Name = "saveFileToPCToolStripMenuItem";
            saveFileToPCToolStripMenuItem.Size = new Size(194, 24);
            saveFileToPCToolStripMenuItem.Text = "Save File To PC";
            saveFileToPCToolStripMenuItem.Click += saveFileToPCToolStripMenuItem_Click;
            // 
            // fileStructureInfoToolStripMenuItem
            // 
            fileStructureInfoToolStripMenuItem.Name = "fileStructureInfoToolStripMenuItem";
            fileStructureInfoToolStripMenuItem.Size = new Size(194, 24);
            fileStructureInfoToolStripMenuItem.Text = "File Structure Info";
            fileStructureInfoToolStripMenuItem.Click += fileStructureInfoToolStripMenuItem_Click;
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(62, 24);
            aboutToolStripMenuItem.Text = "About";
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 28);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(filesTreeView);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(textEditorControl1);
            splitContainer1.Size = new Size(1450, 777);
            splitContainer1.SplitterDistance = 342;
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
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1450, 827);
            Controls.Add(splitContainer1);
            Controls.Add(statusStrip1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "Form1";
            Text = "Call of Duty Fast File Editor";
            FormClosed += Form1_FormClosed;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
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
        private ToolStripMenuItem beautifyCodeToolStripMenuItem;
        private ToolStripMenuItem compressCodeToolStripMenuItem;
        private ToolStripMenuItem removeCommentsToolStripMenuItem;
        private ToolStripMenuItem checkSyntaxToolStripMenuItem;
        private ToolStripMenuItem undoToolStripMenuItem;
        private ToolStripMenuItem redoToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem saveFileToPCToolStripMenuItem;
        private ToolStripMenuItem fileStructureInfoToolStripMenuItem;
    }
}