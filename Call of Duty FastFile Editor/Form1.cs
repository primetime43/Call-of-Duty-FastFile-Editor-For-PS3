using Call_of_Duty_FastFile_Editor.IO;
using Call_of_Duty_FastFile_Editor.UI;

namespace Call_of_Duty_FastFile_Editor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private string ffFilePath;
        private string zoneFilePath;
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
            }
        }
    }
}