using Call_of_Duty_FastFile_Editor.Constants;
namespace Call_of_Duty_FastFile_Editor.UI
{
    public static class UIManager
    {
        /// <summary>
        /// Sets the main window’s title bar to include the program name, version and the opened .ff path.
        /// </summary>
        public static void SetProgramTitle(this Form mainForm, string fastFilePath)
        {
            string version = ApplicationConstants.ProgramVersion;
            string programName = ApplicationConstants.ProgramName;
            mainForm.Text = $"{programName} - {version} - [{fastFilePath}]";
        }

        /// <summary>
        /// Sets the main window’s title bar to include the program name, version.
        /// </summary>
        public static void SetProgramTitle(this Form mainForm)
        {
            string version = ApplicationConstants.ProgramVersion;
            string programName = ApplicationConstants.ProgramName;
            mainForm.Text = $"{programName} - {version}";
        }

        public static void UpdateLoadedFileNameStatusStrip(ToolStripStatusLabel statusLabel, string fileName, bool isCod4File)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                statusLabel.Visible = false;
                return;
            }

            // Decide the prefix based on the flag
            var gameString = isCod4File ? "COD4" : "COD5";
            statusLabel.Text = $"{gameString}: {Path.GetFileName(fileName)}";
            statusLabel.Visible = true;
        }

        public static void UpdateSelectedFileStatusStrip(ToolStripStatusLabel statusLabel, string fileName)
        {
            if (fileName != null)
            {
                statusLabel.Text = fileName;
                statusLabel.Visible = true;
            }
        }

        public static void UpdateStatusStrip(ToolStripStatusLabel maxSizeLabel, ToolStripStatusLabel currentSizeLabel, int maxSize, int currentSize)
        {
            maxSizeLabel.Text = $"Max Size: {maxSize} (dec)";
            currentSizeLabel.Text = $"Current Size: {currentSize} (dec)";
            currentSizeLabel.ForeColor = currentSize > maxSize ? Color.Red : Color.Black;
            maxSizeLabel.Visible = true;
            currentSizeLabel.Visible = true;
        }

        public static void SetRawFileTreeNodeColors(TreeView treeView)
        {
            foreach (TreeNode node in treeView.Nodes)
            {
                if (node.Text.Contains(".cfg"))
                {
                    node.ForeColor = Color.Black;
                }
                if (node.Text.Contains(".gsc"))
                {
                    node.ForeColor = Color.Blue;
                }
                if (node.Text.Contains(".atr"))
                {
                    node.ForeColor = Color.Green;
                }
                if (node.Text.Contains(".vision"))
                {
                    node.ForeColor = Color.DarkViolet;
                }
                if (node.Text.Contains(".rmb"))
                {
                    node.ForeColor = Color.Brown;
                }
                if (node.Text.Contains(".csc"))
                {
                    node.ForeColor = Color.Red;
                }
            }
        }
    }
}