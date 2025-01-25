using Call_of_Duty_FastFile_Editor.IO;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Call_of_Duty_FastFile_Editor.Models;
using Call_of_Duty_FastFile_Editor.FileOperations;
using Call_of_Duty_FastFile_Editor.UI;
using System.Runtime.CompilerServices;
using static Call_of_Duty_FastFile_Editor.Models.FastFile;

namespace Call_of_Duty_FastFile_Editor.CodeOperations
{
    public class RawFileOperations
    {
        public static void ExportRawFile(ZoneAsset_RawFileNode exportedRawFile, string fileExtension)
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

        /// <summary>
        /// Renames a raw file within the Fast File.
        /// </summary>
        /// <param name="filesTreeView">The TreeView containing the files.</param>
        /// <param name="ffFilePath">Path to the Fast File.</param>
        /// <param name="zoneFilePath">Path to the Zone File.</param>
        /// <param name="rawFileNodes">List of RawFileNodes.</param>
        /// <param name="headerInfo">Fast File header information.</param>
        public static void RenameRawFile(TreeView filesTreeView, string ffFilePath, string zoneFilePath, List<ZoneAsset_RawFileNode> rawFileNodes, FastFile openedFastFile)
        {
            try
            {
                if (filesTreeView.SelectedNode?.Tag is int position)
                {
                    var rawFileNode = rawFileNodes.FirstOrDefault(node => node.PatternIndexPosition == position);
                    if (rawFileNode != null)
                    {
                        // Prompt user for new file name
                        string newFileName = PromptForNewFileName(rawFileNode.FileName);
                        if (string.IsNullOrWhiteSpace(newFileName))
                        {
                            MessageBox.Show("Rename operation was canceled or an invalid name was provided.", "Rename Canceled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }

                        // Sanitize the new file name
                        newFileName = SanitizeFileName(newFileName);

                        // Calculate the difference in bytes between new and old file names
                        byte[] oldFileNameBytes = Encoding.ASCII.GetBytes(rawFileNode.FileName);
                        byte[] newFileNameBytes = Encoding.ASCII.GetBytes(newFileName);
                        int byteDifference = newFileNameBytes.Length - oldFileNameBytes.Length;

                        // Read the entire Zone File into memory
                        byte[] zoneFileData = File.ReadAllBytes(zoneFilePath);

                        // Calculate the exact position of the file name in the Zone File
                        int fileNameStartPosition = rawFileNode.StartOfFileHeader + 8; // Assuming header is 8 bytes

                        // Create a backup before making changes
                        CreateBackup(zoneFilePath);

                        if (byteDifference == 0)
                        {
                            // If the new name is the same length, overwrite directly
                            Array.Copy(newFileNameBytes, 0, zoneFileData, fileNameStartPosition, newFileNameBytes.Length);
                            zoneFileData[fileNameStartPosition + newFileNameBytes.Length] = 0x00; // Null terminator
                        }
                        else if (byteDifference < 0)
                        {
                            // New name is shorter; overwrite and shift the remaining bytes left
                            Array.Copy(newFileNameBytes, 0, zoneFileData, fileNameStartPosition, newFileNameBytes.Length);
                            zoneFileData[fileNameStartPosition + newFileNameBytes.Length] = 0x00; // Null terminator

                            // Shift the remaining data
                            int shiftAmount = -byteDifference;
                            Array.Copy(
                                zoneFileData,
                                fileNameStartPosition + oldFileNameBytes.Length + 1,
                                zoneFileData,
                                fileNameStartPosition + newFileNameBytes.Length + 1,
                                zoneFileData.Length - (fileNameStartPosition + oldFileNameBytes.Length + 1)
                            );

                            // Resize the array
                            zoneFileData = zoneFileData.Take(zoneFileData.Length - shiftAmount).ToArray();
                        }
                        else
                        {
                            // New name is longer; insert the extra bytes and shift the remaining bytes right
                            Array.Resize(ref zoneFileData, zoneFileData.Length + byteDifference);
                            Array.Copy(
                                zoneFileData,
                                fileNameStartPosition + oldFileNameBytes.Length + 1,
                                zoneFileData,
                                fileNameStartPosition + newFileNameBytes.Length + 1,
                                zoneFileData.Length - (fileNameStartPosition + oldFileNameBytes.Length + 1 + byteDifference)
                            );

                            Array.Copy(newFileNameBytes, 0, zoneFileData, fileNameStartPosition, newFileNameBytes.Length);
                            zoneFileData[fileNameStartPosition + newFileNameBytes.Length] = 0x00; // Null terminator
                        }

                        // Write the modified Zone File back to disk
                        File.WriteAllBytes(zoneFilePath, zoneFileData);

                        // Update the in-memory RawFileNode
                        rawFileNode.FileName = newFileName;

                        // Update the TreeView node's text to the new file name
                        UpdateTreeViewNodeText(filesTreeView, rawFileNode.PatternIndexPosition, newFileName);

                        MessageBox.Show($"File successfully renamed to '{newFileName}'.", "Rename Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show($"Failed to rename file: {ex.Message}", "Rename Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Prompts the user to enter a new file name.
        /// </summary>
        /// <param name="currentName">The current file name.</param>
        /// <returns>The new file name entered by the user.</returns>
        private static string PromptForNewFileName(string currentName)
        {
            using (RenameDialog renameDialog = new RenameDialog(currentName))
            {
                if (renameDialog.ShowDialog() == DialogResult.OK)
                {
                    return renameDialog.NewFileName;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Updates the TreeView node's text based on the PatternIndexPosition.
        /// </summary>
        /// <param name="filesTreeView">The TreeView control.</param>
        /// <param name="patternIndexPosition">The PatternIndexPosition of the RawFileNode.</param>
        /// <param name="newFileName">The new file name to set.</param>
        private static void UpdateTreeViewNodeText(TreeView filesTreeView, int patternIndexPosition, string newFileName)
        {
            foreach (TreeNode node in filesTreeView.Nodes)
            {
                if (node.Tag is int tag && tag == patternIndexPosition)
                {
                    node.Text = newFileName;
                    break; // Exit the loop once the node is found and updated
                }
            }
        }

        /// <summary>
        /// Saves the updated content of the currently selected raw file to the Zone File and recompresses it back into the Fast File.
        /// </summary>
        /// <param name="filesTreeView">The TreeView control containing the list of raw files.</param>
        /// <param name="ffFilePath">The file path to the Fast File (.ff) being edited.</param>
        /// <param name="zoneFilePath">The file path to the decompressed Zone File (.zone) corresponding to the Fast File.</param>
        /// <param name="rawFileNodes">A list of <see cref="ZoneAsset_RawFileNode"/> objects representing the raw files within the Zone File.</param>
        /// <param name="updatedText">The updated content for the selected raw file, as edited by the user.</param>
        /// <param name="headerInfo">An instance of <see cref="FastFileHeader"/> containing header information of the Fast File.</param>
        /// <exception cref="ArgumentException">Thrown when the updated content size exceeds the original maximum size of the raw file.</exception>
        /// <exception cref="IOException">Thrown when file read/write operations fail.</exception>
        /// <remarks>
        /// This method performs the following steps:
        /// <list type="number">
        ///     <item>Validates that a raw file is selected in the TreeView.</item>
        ///     <item>Retrieves the corresponding <see cref="ZoneAsset_RawFileNode"/> based on the selected node's position.</item>
        ///     <item>Delegates the actual saving process to <see cref="SaveFileNode"/>.</item>
        /// </list>
        /// </remarks>
        public static void Save(TreeView filesTreeView, string ffFilePath, string zoneFilePath, List<ZoneAsset_RawFileNode> rawFileNodes, string updatedText, FastFile openedFastFile)
        {
            try
            {
                if (filesTreeView.SelectedNode?.Tag is int position)
                {
                    var rawFileNode = rawFileNodes.FirstOrDefault(node => node.PatternIndexPosition == position);
                    if (rawFileNode != null)
                    {
                        SaveFileNode(ffFilePath, zoneFilePath, rawFileNode, updatedText, openedFastFile.OpenedFastFileHeader);
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

        /// <summary>
        /// Updates the content of a specific raw file within the Zone File.
        /// </summary>
        /// <param name="ffFilePath">The file path to the Fast File (.ff) being edited.</param>
        /// <param name="zoneFilePath">The file path to the decompressed Zone File (.zone) corresponding to the Fast File.</param>
        /// <param name="rawFileNode">The <see cref="ZoneAsset_RawFileNode"/> object representing the raw file to be updated.</param>
        /// <param name="updatedText">The new content for the raw file, as edited by the user.</param>
        /// <param name="headerInfo">An instance of <see cref="FastFileHeader"/> containing header information of the Fast File.</param>
        /// <exception cref="ArgumentException">Thrown when the updated content size exceeds the original maximum size of the raw file.</exception>
        /// <exception cref="IOException">Thrown when file read/write operations fail.</exception>
        /// <remarks>
        /// This method performs the following operations:
        /// <list type="number">
        ///     <item>Converts the updated text to a byte array using ASCII encoding.</item>
        ///     <item>Validates that the size of the updated content does not exceed the original maximum size.</item>
        ///     <item>Creates a backup of the Zone File before making any changes.</item>
        ///     <item>Updates the Zone File with the new content at the specified position.</item>
        ///     <item>Updates the in-memory <see cref="ZoneAsset_RawFileNode"/> with the new content.</item>
        ///     <item>Notifies the user of the successful save operation to the raw file.</item>
        /// </list>
        /// </remarks>
        private static void SaveFileNode(string ffFilePath, string zoneFilePath, ZoneAsset_RawFileNode rawFileNode, string updatedText, FastFileHeader headerInfo)
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

                MessageBox.Show($"Raw File '{rawFileNode.FileName}' successfully saved to Zone.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
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
        /// Creates a backup of the specified file.
        /// </summary>
        /// <param name="filePath">The path of the file to backup.</param>
        private static void CreateBackup(string filePath)
        {
            string backupPath = $"{filePath}.backup";
            try
            {
                File.Copy(filePath, backupPath, overwrite: true);
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