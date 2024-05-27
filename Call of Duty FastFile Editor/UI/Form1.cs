using Call_of_Duty_FastFile_Editor.CodeOperations;
using Call_of_Duty_FastFile_Editor.IO;
using Call_of_Duty_FastFile_Editor.UI;
using System.Text.RegularExpressions;
using System.Net;
using System.Text;

namespace Call_of_Duty_FastFile_Editor
{
    public partial class Form1 : Form
    {
        private UndoRedo _undoRedoManager;
        public Form1()
        {
            InitializeComponent();
            textEditorControl1.SetHighlighting("C#");
            //_undoRedoManager = new UndoRedo();

            //TrackChange();
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
        private List<FileEntryNode> fileEntryNodes;

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

            var header = new FastFileHeader(ffFilePath);
            if (header.IsValid)
            {
                FastFileProcessing.DecompressFastFile(ffFilePath, zoneFilePath);
                fileEntryNodes = FastFileProcessing.ExtractFileEntriesWithSizeAndName(zoneFilePath);
                filesTreeView.Nodes.AddRange(fileEntryNodes.Select(node => node.Node).ToArray());
            }

            UIManager.SetTreeNodeColors(filesTreeView);

            // move this to the UIManager eventually
            saveRawFileToolStripMenuItem.Enabled = true;
            renameRawFileToolStripMenuItem.Enabled = true;
        }

        private void filesTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag is int position)
            {
                string fileName = e.Node.Text; // Get the selected file name
                var selectedNode = fileEntryNodes.FirstOrDefault(node => node.Position == position);
                int maxSize = selectedNode?.MaxSize ?? 0;
                string fileContent = FastFileProcessing.ReadFileContentAfterName(zoneFilePath, position, maxSize);

                textEditorControl1.TextChanged -= textEditorControl1_TextChanged; // Unsubscribe to prevent multiple triggers
                textEditorControl1.Text = fileContent;
                textEditorControl1.TextChanged += textEditorControl1_TextChanged; // Resubscribe

                UIManager.UpdateSelectedFileStatusStrip(selectedItemStatusLabel, fileName);
                UIManager.UpdateStatusStrip(selectedFileMaxSizeStatusLabel, selectedFileCurrentSizeStatusLabel, maxSize, fileContent.Length);
            }
        }

        private void textEditorControl1_TextChanged(object sender, EventArgs e)
        {
            if (filesTreeView.SelectedNode?.Tag is int position)
            {
                var selectedNode = fileEntryNodes.FirstOrDefault(node => node.Position == position);
                int maxSize = selectedNode?.MaxSize ?? 0;
                UIManager.UpdateStatusStrip(selectedFileMaxSizeStatusLabel, selectedFileCurrentSizeStatusLabel, maxSize, textEditorControl1.Text.Length);

                // Track changes for undo/redo functionality
                //TrackChange();
            }
        }

        private void saveFastFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //FastFileProcessing.RecompressFastFile(ffFilePath, zoneFilePath, fileEntryNodes);
            FastFileProcessing.RecompressFastFile(ffFilePath, zoneFilePath);
            MessageBox.Show("Fast File saved to:\n\n" + ffFilePath, "Saved", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            Application.Restart();
        }

        private void saveFastFileAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Fast Files (*.ff)|*.ff|All Files (*.*)|*.*";
                saveFileDialog.Title = "Save Fast File As";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string newFilePath = saveFileDialog.FileName;
                    //FastFileProcessing.RecompressFastFile(ffFilePath, newFilePath, fileEntryNodes);
                    FastFileProcessing.RecompressFastFile(ffFilePath, newFilePath);
                    MessageBox.Show("Fast File saved to:\n\n" + newFilePath, "Saved", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    Application.Restart();
                }
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (File.Exists(zoneFilePath))
            {
                File.Delete(zoneFilePath);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void saveRawFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveRawFile.Save(filesTreeView, zoneFilePath, fileEntryNodes, textEditorControl1.Text);
        }

        private void removeCommentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textEditorControl1.Text = CommentRemover.RemoveCStyleComments(textEditorControl1.Text);
            textEditorControl1.Text = CommentRemover.RemoveCustomComments(textEditorControl1.Text);
            textEditorControl1.Text = Regex.Replace(textEditorControl1.Text, "(\\r\\n){2,}", "\r\n\r\n");
        }

        private void UndoRedoManager_UndoRedoStackChanged(object sender, EventArgs e)
        {
            undoToolStripMenuItem.Enabled = _undoRedoManager.CanUndo;
            redoToolStripMenuItem.Enabled = _undoRedoManager.CanRedo;
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            /*textEditorControl1.ResetText();
            textEditorControl1.Text = _undoRedoManager.Undo(textEditorControl1.Text);*/
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            /*textEditorControl1.ResetText();
            textEditorControl1.Text = _undoRedoManager.Redo(textEditorControl1.Text);*/
        }

        private void TrackChange()
        {
            _undoRedoManager.TrackChange(textEditorControl1.Text);
        }

        private void saveFileToPCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = Regex.Replace(filesTreeView.SelectedNode.Text, "/", "\\");
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Choose where to save Asset...";
            saveFileDialog.FileName = Path.GetFileName(path);
            saveFileDialog.Filter = Path.GetExtension(path) + " files|*" + Path.GetExtension(path);
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                textEditorControl1.SaveFile(saveFileDialog.FileName);
                MessageBox.Show("File " + Path.GetFileName(saveFileDialog.FileName) + " saved to:\n" + saveFileDialog.FileName, "File Saved.", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }
    }
}