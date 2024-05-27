using Call_of_Duty_FastFile_Editor.IO;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace Call_of_Duty_FastFile_Editor.CodeOperations
{
    public class SaveRawFile
    {
        public static void Save(TreeView filesTreeView, string zoneFilePath, List<FileEntryNode> fileEntryNodes, string updatedText)
        {
            try
            {
                if (filesTreeView.SelectedNode?.Tag is int position)
                {
                    var fileEntryNode = fileEntryNodes.FirstOrDefault(node => node.Position == position);
                    if (fileEntryNode != null)
                    {
                        SaveFileNode(zoneFilePath, fileEntryNode, updatedText);
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

        private static void SaveFileNode(string zoneFilePath, FileEntryNode fileEntryNode, string updatedText)
        {
            int originalSize;
            using (BinaryReader binaryReader = new BinaryReader(new FileStream(zoneFilePath, FileMode.Open, FileAccess.ReadWrite), Encoding.Default))
            {
                binaryReader.BaseStream.Position = fileEntryNode.Position;
                originalSize = IPAddress.NetworkToHostOrder(binaryReader.ReadInt32());
            }

            using (BinaryWriter binaryWriter = new BinaryWriter(new FileStream(zoneFilePath, FileMode.Open, FileAccess.ReadWrite), Encoding.Default))
            {
                // Calculate the position to write the updated content
                binaryWriter.BaseStream.Position = fileEntryNode.Position + 8 + fileEntryNode.Node.Text.Length + 1;
                byte[] updatedBytes = Encoding.ASCII.GetBytes(updatedText);
                int updatedSize = updatedBytes.Length;

                if (updatedSize > originalSize)
                {
                    MessageBox.Show($"New size is {updatedSize - originalSize} bytes larger than original size", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                    return;
                }

                binaryWriter.Write(updatedBytes);

                // Pad with zeros if the new data is shorter than the original data
                if (updatedSize < originalSize)
                {
                    binaryWriter.Write(new byte[originalSize - updatedSize]);
                }

                MessageBox.Show("Raw File Saved To Zone.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }
    }
}