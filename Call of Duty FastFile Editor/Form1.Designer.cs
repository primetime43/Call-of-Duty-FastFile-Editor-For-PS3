using ICSharpCode.TextEditor;

namespace Call_of_Duty_FastFile_Editor
{
    partial class Form1
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
            filesTreeView = new TreeView();
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            openFastFileToolStripMenuItem = new ToolStripMenuItem();
            splitContainer1 = new SplitContainer();
            statusStrip1 = new StatusStrip();
            loadedFileNameStatusLabel = new ToolStripStatusLabel();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            selectedItemStatusLabel = new ToolStripStatusLabel();
            selectedFileMaxSizeStatusLabel = new ToolStripStatusLabel();
            selectedFileCurrentSizeStatusLabel = new ToolStripStatusLabel();
            textEditorControl1 = new TextEditorControl();
            menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
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
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1450, 28);
            menuStrip1.TabIndex = 1;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openFastFileToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(44, 24);
            fileToolStripMenuItem.Text = "File";
            // 
            // openFastFileToolStripMenuItem
            // 
            openFastFileToolStripMenuItem.Name = "openFastFileToolStripMenuItem";
            openFastFileToolStripMenuItem.Size = new Size(170, 24);
            openFastFileToolStripMenuItem.Text = "Open Fast File";
            openFastFileToolStripMenuItem.Click += openFastFileToolStripMenuItem_Click;
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
            // textEditorControl1
            // 
            textEditorControl1.Dock = DockStyle.Fill;
            textEditorControl1.IsReadOnly = false;
            textEditorControl1.Location = new Point(0, 0);
            textEditorControl1.Name = "textEditorControl1";
            textEditorControl1.Size = new Size(1104, 777);
            textEditorControl1.TabIndex = 0;
            textEditorControl1.SetHighlighting("C#");
            textEditorControl1.TextChanged += new System.EventHandler(this.textEditorControl1_TextChanged);
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
    }
}