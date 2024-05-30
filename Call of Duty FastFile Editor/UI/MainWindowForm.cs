using Call_of_Duty_FastFile_Editor.CodeOperations;
using Call_of_Duty_FastFile_Editor.IO;
using Call_of_Duty_FastFile_Editor.UI;
using System.Text.RegularExpressions;
using System.Net;
using System.Text;
using System.Reflection;
using Call_of_Duty_FastFile_Editor.Original_Fast_Files;

namespace Call_of_Duty_FastFile_Editor
{
    public partial class MainWindowForm : Form
    {
        private string _originalFastFilesPath = Path.Combine(Application.StartupPath, "Original Fast Files");
        public MainWindowForm()
        {
            InitializeComponent();
            textEditorControl1.SetHighlighting("C#");

            DirectoryInfo directoryInfo = new DirectoryInfo(_originalFastFilesPath);
            directoryInfo.Attributes |= FileAttributes.Hidden;
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

        /// <summary>
        /// Header information of the opened Fast File.
        /// </summary>
        private FastFileHeader _header;

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

            _header = new FastFileHeader(ffFilePath);
            if (_header.IsValid)
            {
                FastFileProcessing.DecompressFastFile(ffFilePath, zoneFilePath);
                fileEntryNodes = FastFileProcessing.ExtractFileEntriesWithSizeAndName(zoneFilePath);
                filesTreeView.Nodes.AddRange(fileEntryNodes.Select(node => node.Node).ToArray());
            }

            UIManager.SetTreeNodeColors(filesTreeView);

            // move this to the UIManager eventually
            saveRawFileToolStripMenuItem.Enabled = true;
            renameRawFileToolStripMenuItem.Enabled = true;
            saveFastFileToolStripMenuItem.Enabled = true;
            saveFastFileAsToolStripMenuItem.Enabled = true;
        }

        private void filesTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag is int position)
            {
                string fileName = e.Node.Text; // Get the selected file name
                var selectedNode = fileEntryNodes.FirstOrDefault(node => node.PatternIndexPosition == position);
                int maxSize = selectedNode?.MaxSize ?? 0;
                string fileContent = FastFileProcessing.ReadFileContentAfterName(zoneFilePath, position, maxSize);

                textEditorControl1.TextChanged -= textEditorControl1_TextChanged; // Unsubscribe to prevent multiple triggers
                textEditorControl1.Text = fileContent;
                textEditorControl1.TextChanged += textEditorControl1_TextChanged; // Resubscribe

                UIManager.UpdateSelectedFileStatusStrip(selectedItemStatusLabel, fileName);
                UIManager.UpdateStatusStrip(selectedFileMaxSizeStatusLabel, selectedFileCurrentSizeStatusLabel, maxSize, textEditorControl1.Text.Length);
            }
        }

        private void textEditorControl1_TextChanged(object sender, EventArgs e)
        {
            if (filesTreeView.SelectedNode?.Tag is int position)
            {
                var selectedNode = fileEntryNodes.FirstOrDefault(node => node.PatternIndexPosition == position);
                int maxSize = selectedNode?.MaxSize ?? 0;
                UIManager.UpdateStatusStrip(selectedFileMaxSizeStatusLabel, selectedFileCurrentSizeStatusLabel, maxSize, textEditorControl1.Text.Length);
            }
        }

        private void saveFastFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FastFileProcessing.RecompressFastFile(ffFilePath, zoneFilePath, _header);
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
                    FastFileProcessing.RecompressFastFile(ffFilePath, newFilePath, _header);
                    MessageBox.Show("Fast File saved to:\n\n" + newFilePath, "Saved", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    Application.Restart();
                }
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Deleting the zone file of the opened ff file
            if (File.Exists(zoneFilePath))
            {
                File.Delete(zoneFilePath);
            }

            // Deleting the Original Fast Files directory
            if (Directory.Exists(_originalFastFilesPath))
            {
                try
                {
                    Directory.Delete(_originalFastFilesPath, true);
                }
                catch { }
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

        private void fileStructureInfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (filesTreeView.SelectedNode != null)
            {
                if (filesTreeView.SelectedNode.Tag is int position)
                {
                    string fileName = filesTreeView.SelectedNode.Text; // Get the selected file name
                    var selectedFileNode = fileEntryNodes.FirstOrDefault(node => node.PatternIndexPosition == position);

                    // Additional logic for handling the selected file node
                    if (selectedFileNode != null)
                    {
                        new FileStructureInfoForm(selectedFileNode).Show();
                    }
                    else
                    {
                        MessageBox.Show("Selected file node not found in file entry nodes.");
                    }
                }
                else
                {
                    MessageBox.Show("Selected node does not have a valid position.");
                }
            }
            else
            {
                MessageBox.Show("No node is selected.");
            }
        }

        private void compressCodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textEditorControl1.Text = CommentRemover.RemoveCStyleComments(textEditorControl1.Text);
            textEditorControl1.Text = CommentRemover.RemoveCustomComments(textEditorControl1.Text);
            textEditorControl1.Text = CodeCompressor.CompressCode(textEditorControl1.Text);
        }

        private void checkSyntaxToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SyntaxChecker.CheckSyntax(textEditorControl1.Text);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string message = "Call of Duty Fast File Editor\n\n" +
                             "Developed by primetime43\n\n" +
                             "Special thanks to:\n" +
                             "- BuC-ShoTz\n" +
                             "- aerosoul94\n" +
                             "- EliteMossy\n\n" +
                             "GitHub: https://github.com/primetime43";
            MessageBox.Show(message, "About Call of Duty Fast File Editor");
        }

        private void defaultffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DownloadManager.DownloadFile("default.ff", Path.Combine("Original Fast Files", "COD5"));
        }

        private void patchmpffToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            DownloadManager.DownloadFile("patch_mp.ff", Path.Combine("Original Fast Files", "COD5"));
        }

        private void nazizombiefactorypatchffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DownloadManager.DownloadFile("nazi_zombie_factory_patch.ff", Path.Combine("Original Fast Files", "COD5"));
        }

        private void patchmpffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DownloadManager.DownloadFile("patch_mp.ff", Path.Combine("Original Fast Files", "COD4"));
        }
    }
}