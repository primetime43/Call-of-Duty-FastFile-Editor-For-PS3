using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Call_of_Duty_FastFile_Editor.IO
{
    public class FileEntryNode
    {
        public TreeNode Node { get; set; }
        public int Position { get; set; }
        public int MaxSize { get; set; }
        public int StartOfGscHeader { get; set; }
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

            // Read the entire file content as a string for regex matching
            string input = Encoding.Default.GetString(fileData);

            // Define the byte patterns to search for
            string[] patterns = new string[]
            {
                "\\x2E\\x63\\x66\\x67\\x00", // .cfg
                "\\x2E\\x67\\x73\\x63\\x00", // .gsc
                "\\x2E\\x61\\x74\\x72\\x00", // .atr
                "\\x2E\\x63\\x73\\x63\\x00", // .csc
                "\\x2E\\x72\\x6D\\x62\\x00", // .rmb
                "\\x2E\\x61\\x72\\x65\\x6E\\x61\\x00", // .arena
                "\\x2E\\x76\\x69\\x73\\x69\\x6F\\x6E\\x00" // .vision
            };

            using (BinaryReader binaryReader = new BinaryReader(new MemoryStream(fileData), Encoding.Default))
            {
                foreach (string pattern in patterns)
                {
                    foreach (Match match in Regex.Matches(input, pattern, RegexOptions.IgnoreCase))
                    {
                        int patternIndex = match.Index;

                        // Move backwards to find the FF FF FF FF sequence
                        int ffffPosition = patternIndex - 1;
                        while (ffffPosition >= 4 && !(fileData[ffffPosition] == 0xFF && fileData[ffffPosition - 1] == 0xFF && fileData[ffffPosition - 2] == 0xFF && fileData[ffffPosition - 3] == 0xFF))
                        {
                            ffffPosition--;
                        }

                        // The size is stored right before the FF FF FF FF sequence
                        int sizePosition = ffffPosition - 7;
                        if (sizePosition >= 0)
                        {
                            binaryReader.BaseStream.Position = sizePosition;
                            int maxSize = IPAddress.NetworkToHostOrder(binaryReader.ReadInt32());

                            // The file name starts right after the FF FF FF FF sequence
                            int fileNameStart = ffffPosition + 1;
                            string fileName = ExtractFullFileName(fileData, fileNameStart);

                            if (!string.IsNullOrEmpty(fileName))
                            {
                                TreeNode treeNode = new TreeNode(fileName)
                                {
                                    Tag = patternIndex
                                };
                                fileEntryNodes.Add(new FileEntryNode { Node = treeNode, Position = patternIndex, MaxSize = maxSize, StartOfGscHeader = sizePosition });

                                // Debugging message box
                                //MessageBox.Show($"File Name: {fileName}\nPattern Index: {patternIndex:X}\nSize Position: {sizePosition:X}\nMax Size: {maxSize}\nHeader Start: {sizePosition:X}", "Debug Info");
                            }
                        }
                    }
                }
            }

            fileEntryNodes.Sort((a, b) => a.Position.CompareTo(b.Position));
            return fileEntryNodes;
        }

        public static string ExtractFullFileName(byte[] data, int fileNameStart)
        {
            StringBuilder fileName = new StringBuilder();

            // Read the file name from fileNameStart until the null terminator
            for (int i = fileNameStart; i < data.Length; i++)
            {
                char c = (char)data[i];
                if (c == '\0')
                {
                    break;
                }
                fileName.Append(c);
            }

            // Debugging message box
            //MessageBox.Show($"Extracted File Name: {fileName}\nFile Name Start: {fileNameStart:X}", "Debug Info");

            return fileName.ToString();
        }

        public static string ReadFileContentAfterName(string filePath, int startPosition, int maxSize)
        {
            byte[] fileData = File.ReadAllBytes(filePath);

            // Move to the position after the name's null terminator
            int contentStartPosition = startPosition;
            while (contentStartPosition < fileData.Length && fileData[contentStartPosition] != 0x00)
            {
                contentStartPosition++;
            }
            // Skip the null terminator
            contentStartPosition++;

            // Calculate content length up to max size
            int contentLength = maxSize;
            if (contentStartPosition + contentLength > fileData.Length)
            {
                contentLength = fileData.Length - contentStartPosition;
            }

            byte[] contentBytes = new byte[contentLength];
            Array.Copy(fileData, contentStartPosition, contentBytes, 0, contentLength);

            return Encoding.Default.GetString(contentBytes);
        }
    }
}