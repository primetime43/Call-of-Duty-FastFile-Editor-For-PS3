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
using System.Net;

namespace Call_of_Duty_FastFile_Editor.CodeOperations
{
    public class RawFileOperations
    {
        /// <summary>
        /// Exports the selected raw file to a stand alone file.
        /// Includes everything needed from the start of the header to the null terminator at the end of the file (Asset record's end position).
        /// </summary>
        /// <param name="exportedRawFile"></param>
        /// <param name="fileExtension"></param>
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

                        // Use the current zone data and the positions stored in the RawFileNode.
                        byte[] zoneData = RawFileNode.CurrentZone.ZoneFileData;
                        int start = exportedRawFile.StartOfFileHeader;
                        int length = exportedRawFile.RawFileEndPosition - exportedRawFile.StartOfFileHeader;
                        byte[] exportBytes = zoneData.Skip(start).Take(length).ToArray();

                        File.WriteAllBytes(exportPath, exportBytes);

                        MessageBox.Show($"File successfully exported to:\n\n{exportPath}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to export file: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        public static void RenameRawFile(TreeView filesTreeView, string ffFilePath, string zoneFilePath, List<RawFileNode> rawFileNodes, FastFile openedFastFile)
        {
            try
            {
                if (filesTreeView.SelectedNode?.Tag is RawFileNode selectedFileNode)
                {
                    var rawFileNode = rawFileNodes.FirstOrDefault(node => node.PatternIndexPosition == selectedFileNode.PatternIndexPosition);
                    if (rawFileNode != null)
                    {
                        // Prompt the user for a new file name.
                        string newFileName = PromptForNewFileName(rawFileNode.FileName);
                        if (string.IsNullOrWhiteSpace(newFileName))
                        {
                            MessageBox.Show("Rename operation was canceled or an invalid name was provided.",
                                "Rename Canceled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }

                        // The header structure is: 4 bytes, then 4 bytes size, then 4 bytes,
                        // then the file name (in ASCII). The file name field is currently of length equal to rawFileNode.FileName.Length.
                        const int fixedHeaderSize = 12;
                        int fileNameStartPosition = rawFileNode.StartOfFileHeader + fixedHeaderSize;

                        // Get the old file name length.
                        int oldNameLength = rawFileNode.FileName.Length;
                        // Get the new file name as bytes (without a null terminator).
                        byte[] newFileNameBytes = rawFileNode.GetFileNameBytes(newFileName);
                        int newNameLength = newFileNameBytes.Length;

                        // Compute the byte difference between the new and old name lengths.
                        int byteDifference = newNameLength - oldNameLength;

                        // Read the entire zone file into memory.
                        byte[] zoneFileData = File.ReadAllBytes(zoneFilePath);

                        // Create a backup before modifying.
                        CreateBackup(zoneFilePath);

                        if (byteDifference == 0)
                        {
                            // Overwrite directly if the lengths are identical.
                            Array.Copy(newFileNameBytes, 0, zoneFileData, fileNameStartPosition, newNameLength);
                        }
                        else if (byteDifference < 0)
                        {
                            // New name is shorter.
                            // Overwrite the file name field with the new bytes.
                            Array.Copy(newFileNameBytes, 0, zoneFileData, fileNameStartPosition, newNameLength);
                            // Fill remaining bytes (if any) with zeros.
                            for (int i = newNameLength; i < oldNameLength; i++)
                            {
                                zoneFileData[fileNameStartPosition + i] = 0;
                            }
                            // Shift the remainder of the zone data left.
                            int shiftStart = fileNameStartPosition + oldNameLength;
                            int shiftLength = zoneFileData.Length - shiftStart;
                            Array.Copy(zoneFileData, shiftStart, zoneFileData, fileNameStartPosition + newNameLength, shiftLength);
                            // Truncate the zone file data array.
                            zoneFileData = zoneFileData.Take(zoneFileData.Length + byteDifference).ToArray();
                        }
                        else // byteDifference > 0
                        {
                            // New name is longer.
                            int originalLength = zoneFileData.Length;
                            // Resize the array to make room for extra bytes.
                            Array.Resize(ref zoneFileData, originalLength + byteDifference);
                            // Shift the remainder of the zone data right.
                            int shiftStart = fileNameStartPosition + oldNameLength;
                            int shiftLength = originalLength - shiftStart;
                            Array.Copy(zoneFileData, shiftStart, zoneFileData, fileNameStartPosition + newNameLength, shiftLength);
                            // Overwrite the file name field with the new bytes.
                            Array.Copy(newFileNameBytes, 0, zoneFileData, fileNameStartPosition, newNameLength);
                        }

                        // Write the modified zone file back to disk.
                        File.WriteAllBytes(zoneFilePath, zoneFileData);

                        // Save the old file name for notification.
                        string oldFileName = rawFileNode.FileName;
                        // Update the renamed file's FileName property.
                        rawFileNode.FileName = newFileName;
                        // Note: Computed properties (CodeStartPosition, CodeEndPosition, RawFileEndPosition) will reflect the new name length.

                        // Adjust the StartOfFileHeader for all subsequent raw file nodes.
                        foreach (var node in rawFileNodes)
                        {
                            if (node.StartOfFileHeader > rawFileNode.StartOfFileHeader)
                            {
                                node.StartOfFileHeader += byteDifference;
                            }
                        }

                        // Update the TreeView node text.
                        UpdateTreeViewNodeText(filesTreeView, rawFileNode.PatternIndexPosition, newFileName);

                        MessageBox.Show($"File successfully renamed from '{oldFileName}' to '{newFileName}'.",
                            "Rename Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Selected node does not match any file entry nodes.",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("No node is selected or the selected node does not have a valid position.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                if (node.Tag is RawFileNode rfn && rfn.PatternIndexPosition == patternIndexPosition)
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
        /// <param name="rawFileNodes">A list of <see cref="RawFileNode"/> objects representing the raw files within the Zone File.</param>
        /// <param name="updatedText">The updated content for the selected raw file, as edited by the user.</param>
        /// <param name="headerInfo">An instance of <see cref="FastFileHeader"/> containing header information of the Fast File.</param>
        /// <exception cref="ArgumentException">Thrown when the updated content size exceeds the original maximum size of the raw file.</exception>
        /// <exception cref="IOException">Thrown when file read/write operations fail.</exception>
        /// <remarks>
        /// This method performs the following steps:
        /// <list type="number">
        ///     <item>Validates that a raw file is selected in the TreeView.</item>
        ///     <item>Retrieves the corresponding <see cref="RawFileNode"/> based on the selected node's position.</item>
        ///     <item>Delegates the actual saving process to <see cref="SaveFileNode"/>.</item>
        /// </list>
        /// </remarks>
        public static void SaveZoneRawFileChanges(TreeView filesTreeView, string ffFilePath, string zoneFilePath, List<RawFileNode> rawFileNodes, string updatedText, FastFile openedFastFile)
        {
            try
            {
                if (filesTreeView.SelectedNode?.Tag is RawFileNode rawFileNode)
                {
                    SaveFileNode(
                      ffFilePath,
                      zoneFilePath,
                      rawFileNode,                 
                      updatedText,
                      openedFastFile.OpenedFastFileHeader
                    );
                }
                else
                {
                    MessageBox.Show(
                      "No node is selected or the selected node does not have a valid RawFileNode.",
                      "Error",
                      MessageBoxButtons.OK,
                      MessageBoxIcon.Error
                    );
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
        /// <param name="rawFileNode">The <see cref="RawFileNode"/> object representing the raw file to be updated.</param>
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
        ///     <item>Updates the in-memory <see cref="RawFileNode"/> with the new content.</item>
        ///     <item>Notifies the user of the successful save operation to the raw file.</item>
        /// </list>
        /// </remarks>
        private static void SaveFileNode(string ffFilePath, string zoneFilePath, RawFileNode rawFileNode, string updatedText, FastFileHeader headerInfo)
        {
            byte[] updatedBytes = Encoding.ASCII.GetBytes(updatedText);
            int updatedSize = updatedBytes.Length;
            int originalSize = rawFileNode.MaxSize;

            // If new content exceeds the current slot, offer to resize
            if (updatedSize > originalSize)
            {
                var result = MessageBox.Show(
                    $"Content is {updatedSize} bytes (max {originalSize}).\n" +
                    "Do you want to expand the slot to fit?",
                    "Resize Raw File Slot",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    // Expand the raw file slot in the zone file
                    RawFileOps.AdjustRawFileNodeSize(zoneFilePath, rawFileNode, updatedSize);
                }
                else
                {
                    // User declined—abort save
                    return;
                }
            }

            try
            {
                // Backup zone file before writing
                CreateBackup(zoneFilePath);

                // Now write the content (will pad with zeros if smaller than MaxSize)
                RawFileOps.UpdateFileContent(zoneFilePath, rawFileNode, updatedBytes);
                rawFileNode.RawFileContent = updatedText;

                MessageBox.Show(
                    $"Raw File '{rawFileNode.FileName}' saved successfully.",
                    "Saved",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Asterisk);
            }
            catch (IOException ioEx)
            {
                MessageBox.Show($"Failed to save raw file: {ioEx.Message}", "IO Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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