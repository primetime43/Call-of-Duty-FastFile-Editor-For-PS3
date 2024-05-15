using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Call_of_Duty_FastFile_Viewer.IO
{
    public class FileEntryNode
    {
        public TreeNode Node { get; set; }
        public int Position { get; set; }
    }

    public static class FastFileProcessing
    {
        public static void DecompressFastFile(string inputFilePath, string outputFilePath)
        {
            using (BinaryReader binaryReader = new BinaryReader(new FileStream(inputFilePath, FileMode.Open), Encoding.Default))
            using (BinaryWriter binaryWriter = new BinaryWriter(new FileStream(outputFilePath, FileMode.Create), Encoding.Default))
            {
                binaryReader.BaseStream.Position = 12L;

                try
                {
                    for (int i = 1; i < 5000; i++)
                    {
                        byte[] array = binaryReader.ReadBytes(2);
                        string text = BitConverter.ToString(array).Replace("-", "");
                        int count = int.Parse(text, System.Globalization.NumberStyles.AllowHexSpecifier);
                        byte[] compressedData = binaryReader.ReadBytes(count);
                        byte[] decompressedData = FastFileDecompressor.DecompressFF(compressedData);
                        binaryWriter.Write(decompressedData);
                    }
                }
                catch (Exception ex)
                {
                    if (!(ex is FormatException))
                    {
                        throw;
                    }
                }
            }
        }

        public static List<FileEntryNode> LocateGscFileEntries(string filePath)
        {
            List<FileEntryNode> fileEntryNodes = new List<FileEntryNode>();
            byte[] fileData = File.ReadAllBytes(filePath);

            // Define the byte patterns to search for
            byte[][] patterns = new byte[][]
            {
                new byte[] { 0x2E, 0x63, 0x66, 0x67, 0x00 }, // .cfg
                new byte[] { 0x2E, 0x67, 0x73, 0x63, 0x00 }, // .gsc
                new byte[] { 0x2E, 0x61, 0x74, 0x72, 0x00 }, // .atr
                new byte[] { 0x2E, 0x63, 0x73, 0x63, 0x00 }, // .csc
                new byte[] { 0x2E, 0x72, 0x6D, 0x62, 0x00 }, // .rmb
                new byte[] { 0x2E, 0x61, 0x72, 0x65, 0x6E, 0x61, 0x00 }, // .arena
                new byte[] { 0x2E, 0x76, 0x69, 0x73, 0x69, 0x6F, 0x6E, 0x00 } // .vision
            };

            foreach (byte[] pattern in patterns)
            {
                List<int> positions = FilePatternFinder.FindBytePattern(fileData, pattern);
                foreach (int position in positions)
                {
                    string fileName = FilePatternFinder.ExtractFullFileName(fileData, position);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        TreeNode treeNode = new TreeNode(fileName)
                        {
                            Tag = position
                        };
                        fileEntryNodes.Add(new FileEntryNode { Node = treeNode, Position = position });
                    }
                }
            }

            fileEntryNodes.Sort((a, b) => a.Position.CompareTo(b.Position));
            return fileEntryNodes;
        }

        public static string ReadFileContentAfterName(string filePath, int startPosition)
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            int contentStartPosition = startPosition;

            // Move to the position after the name
            while (contentStartPosition < fileData.Length && fileData[contentStartPosition] != 0x00)
            {
                contentStartPosition++;
            }

            // Skip the null terminator
            contentStartPosition++;

            // Define the end pattern
            byte[] endPattern = new byte[] { 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00 };

            // Find the end position
            int contentEndPosition = FilePatternFinder.FindPattern(fileData, contentStartPosition, endPattern);

            // Calculate the length of the content
            int contentLength = contentEndPosition - contentStartPosition;
            if (contentLength < 0)
            {
                contentLength = 0;
            }

            // Read the content after the name up to the end position
            byte[] contentBytes = new byte[contentLength];
            Array.Copy(fileData, contentStartPosition, contentBytes, 0, contentLength);

            return Encoding.Default.GetString(contentBytes);
        }
    }
}
