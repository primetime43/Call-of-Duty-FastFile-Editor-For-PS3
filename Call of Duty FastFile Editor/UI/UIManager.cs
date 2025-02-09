namespace Call_of_Duty_FastFile_Editor.UI
{
    public static class UIManager
    {
        public static void UpdateLoadedFileNameStatusStrip(ToolStripStatusLabel statusLabel, string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                statusLabel.Text = Path.GetFileName(filePath);
                statusLabel.Visible = true;
            }
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