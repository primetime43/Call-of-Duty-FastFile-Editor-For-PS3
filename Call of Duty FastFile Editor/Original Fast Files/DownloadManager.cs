using System;
using System.IO;
using System.Windows.Forms;

namespace Call_of_Duty_FastFile_Editor.Original_Fast_Files
{
    public static class DownloadManager
    {
        public static void DownloadFile(string fileName, string sourceDirectory, string filter = "Fast Files (*.ff)|*.ff")
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                FileName = fileName,
                Filter = filter
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string sourcePath = Path.Combine(Application.StartupPath, sourceDirectory, fileName);
                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, saveFileDialog.FileName, true);
                    MessageBox.Show($"{fileName} Fast File saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
                else
                {
                    MessageBox.Show("The source file could not be found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}