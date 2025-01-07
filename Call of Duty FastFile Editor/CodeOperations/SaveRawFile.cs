using Call_of_Duty_FastFile_Editor.IO;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Call_of_Duty_FastFile_Editor.Models;
using Call_of_Duty_FastFile_Editor.FileOperations;

namespace Call_of_Duty_FastFile_Editor.CodeOperations
{
    public class SaveRawFile
    {
        public static void Save(TreeView filesTreeView, string ffFilePath, string zoneFilePath, List<RawFileNode> rawFileNodes, string updatedText, FastFileHeader headerInfo)
        {
            try
            {
                if (filesTreeView.SelectedNode?.Tag is int position)
                {
                    var rawFileNode = rawFileNodes.FirstOrDefault(node => node.PatternIndexPosition == position);
                    if (rawFileNode != null)
                    {
                        SaveFileNode(ffFilePath, zoneFilePath, rawFileNode, updatedText, headerInfo);
                    }
                    else
                    {
                        MessageBox.Show("Selected node does not match any file entry nodes.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("No node is selected or the selected node does not have a valid position.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void SaveFileNode(string ffFilePath, string zoneFilePath, RawFileNode rawFileNode, string updatedText, FastFileHeader headerInfo)
        {
            byte[] updatedBytes = Encoding.ASCII.GetBytes(updatedText);
            int updatedSize = updatedBytes.Length;
            int originalSize = rawFileNode.MaxSize;

            if (updatedSize > originalSize)
            {
                MessageBox.Show($"New size is {updatedSize - originalSize} bytes larger than the original size.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }

            try
            {
                // Create a backup before making changes
                CreateBackup(ffFilePath);

                // Update the zone file in memory
                RawFileInject.UpdateFileContent(zoneFilePath, rawFileNode, updatedBytes);
                rawFileNode.RawFileContent = updatedText; // Update the in-memory representation

                // Recompress the modified zone file back into the Fast File
                FastFileProcessing.RecompressFastFile(ffFilePath, zoneFilePath, headerInfo);

                MessageBox.Show($"Raw File '{rawFileNode.FileName}' successfully saved to Zone and Fast File.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            catch (ArgumentException argEx)
            {
                MessageBox.Show(argEx.Message, "Size Exceeded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (IOException ioEx)
            {
                MessageBox.Show($"Failed to save file: {ioEx.Message}", "IO Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void CreateBackup(string originalFilePath)
        {
            string backupPath = $"{originalFilePath}.backup";
            try
            {
                File.Copy(originalFilePath, backupPath, overwrite: true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create backup: {ex.Message}", "Backup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}