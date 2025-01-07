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
    public class RawFileOperations
    {
        public static void ExportRawFile(RawFileNode exportedRawFile, string fileExtension)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Title = "Export File";
                saveFileDialog.FileName = SanitizeFileName(exportedRawFile.FileName);
                saveFileDialog.Filter = $"{fileExtension.TrimStart('.').ToUpper()} Files (*{fileExtension})|*{fileExtension}|All Files (*.*)|*.*";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string exportPath = saveFileDialog.FileName;

                        using (FileStream fs = new FileStream(exportPath, FileMode.Create, FileAccess.Write))
                        using (BinaryWriter bw = new BinaryWriter(fs))
                        {
                            // Write the header (size and padding)
                            bw.Write(exportedRawFile.Header);

                            // Write the file name in ASCII
                            byte[] fileNameBytes = Encoding.ASCII.GetBytes(exportedRawFile.FileName);
                            bw.Write(fileNameBytes);

                            // Write a null terminator (0x00) after the file name
                            bw.Write((byte)0x00);

                            // Write the file content
                            byte[] contentBytes = exportedRawFile.RawFileBytes ?? Encoding.UTF8.GetBytes(exportedRawFile.RawFileContent ?? string.Empty);
                            bw.Write(contentBytes);

                            // Write padding (00 FF FF FF FF) at the end
                            //bw.Write(new byte[] { 0x00, 0xFF, 0xFF, 0xFF, 0xFF });

                            // Write padding (00) at the end
                            bw.Write(new byte[] { 0x00 });
                        }

                        MessageBox.Show($"File successfully exported to:\n\n{exportPath}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to export file: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

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

        /// <summary>
        /// Create a backup of the fast file before making changes.
        /// </summary>
        /// <param name="originalFilePath"></param>
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

        /// <summary>
        /// Sanitizes a filename by replacing invalid characters with underscores.
        /// </summary>
        /// <param name="fileName">The original filename.</param>
        /// <returns>A sanitized filename safe for the filesystem.</returns>
        private static string SanitizeFileName(string fileName)
        {
            // Retrieve all invalid characters for filenames
            char[] invalidChars = Path.GetInvalidFileNameChars();

            // Replace each invalid character with an underscore
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }

            return fileName;
        }
    }
}