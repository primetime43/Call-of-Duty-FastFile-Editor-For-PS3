using Call_of_Duty_FastFile_Editor.IO;

namespace Call_of_Duty_FastFile_Editor.UI
{
    // NOTE: 0D 0A is a line break in hex
    public partial class FileStructureInfoForm : Form
    {
        private FileEntryNode _selectedFileNode;

        public FileStructureInfoForm()
        {
            InitializeComponent();
        }

        public FileStructureInfoForm(FileEntryNode selectedFileNode)
        {
            InitializeComponent();

            _selectedFileNode = selectedFileNode;
            decimalRadioButton.Checked = true;

            DisplayFileInfo();
        }

        private void DisplayFileInfo()
        {
            selectedFileNameTextBox.Text = _selectedFileNode.FileName;
            codeStartPositionTextBox.Text = _selectedFileNode.CodeStartPosition.ToString("D");
            startOfFileHeaderPositionTextBox.Text = _selectedFileNode.StartOfFileHeader.ToString("D");
            fileSizePositionTextBox.Text = _selectedFileNode.StartOfFileHeader.ToString("D");
            maxFileSizeTextBox.Text = _selectedFileNode.MaxSize.ToString("D");
            endOfFIlePositionTextBox.Text = _selectedFileNode.CodeEndPosition.ToString("D");
        }

        private void decimalRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (decimalRadioButton.Checked)
            {
                codeStartPositionTextBox.Text = _selectedFileNode.CodeStartPosition.ToString("D");
                startOfFileHeaderPositionTextBox.Text = _selectedFileNode.StartOfFileHeader.ToString("D");
                fileSizePositionTextBox.Text = _selectedFileNode.StartOfFileHeader.ToString("D");
                maxFileSizeTextBox.Text = _selectedFileNode.MaxSize.ToString("D");
                endOfFIlePositionTextBox.Text = _selectedFileNode.CodeEndPosition.ToString("D");
            }
        }

        private void hexadecimalRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (hexadecimalRadioButton.Checked)
            {
                codeStartPositionTextBox.Text = _selectedFileNode.CodeStartPosition.ToString("X");
                startOfFileHeaderPositionTextBox.Text = _selectedFileNode.StartOfFileHeader.ToString("X");
                fileSizePositionTextBox.Text = _selectedFileNode.StartOfFileHeader.ToString("X");
                maxFileSizeTextBox.Text = _selectedFileNode.MaxSize.ToString("X");
                endOfFIlePositionTextBox.Text = _selectedFileNode.CodeEndPosition.ToString("X");
            }
        }
    }
}