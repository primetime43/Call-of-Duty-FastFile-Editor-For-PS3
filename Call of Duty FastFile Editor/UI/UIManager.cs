using FastColoredTextBoxNS;

namespace Call_of_Duty_FastFile_Viewer.UI
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

        public static void SetTreeNodeColors(TreeView treeView)
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

        public static void InitializeFastColoredTextBoxSyntaxHighlighting(FastColoredTextBox fastColoredTextBox)
        {
            TextStyle commentStyle = new TextStyle(Brushes.Green, null, FontStyle.Italic);
            TextStyle keywordStyle = new TextStyle(Brushes.Blue, null, FontStyle.Bold);
            TextStyle stringStyle = new TextStyle(Brushes.Brown, null, FontStyle.Regular);

            fastColoredTextBox.TextChanged += (sender, args) =>
            {
                var range = fastColoredTextBox.Range;

                range.ClearStyle(commentStyle, keywordStyle, stringStyle);

                range.SetStyle(commentStyle, @"//.*|/\*[\s\S]*?\*/");
                range.SetStyle(stringStyle, @"""(\\.|[^""\\])*""|'(\\.|[^'\\])*'");

                string[] keywords = { "if", "else", "while", "for", "return", "function", "level", "self", "thread" };
                foreach (var keyword in keywords)
                {
                    range.SetStyle(keywordStyle, $@"\b{keyword}\b");
                }
            };
        }
    }
}
