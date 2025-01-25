using System;
using System.IO;
using System.Text;
using Call_of_Duty_FastFile_Editor.Models; // Added namespace

namespace Call_of_Duty_FastFile_Editor.IO
{
    public static class FastFileExport
    {
        /// <summary>
        /// Exports the specified file entry to the desired destination path.
        /// </summary>
        /// <param name="rawFileNode">The raw file node in the opened FF to export.</param>
        /// <param name="destinationPath">The full path where the file will be exported.</param>
        public static void ExportRawFile(ZoneAsset_RawFileNode rawFileNode, string destinationPath)
        {
            if (rawFileNode == null)
                throw new ArgumentNullException(nameof(rawFileNode), "Raw file node cannot be null.");

            if (string.IsNullOrWhiteSpace(destinationPath))
                throw new ArgumentException("Destination path cannot be null or whitespace.", nameof(destinationPath));

            try
            {
                ExportRawFileNode(rawFileNode, destinationPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to export file '{rawFileNode.FileName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Exports a binary file.
        /// </summary>
        /// <param name="rawFileNode">The raw file node to be exported.</param>
        /// <param name="destinationPath">The full path where the file will be exported.</param>
        private static void ExportRawFileNode(ZoneAsset_RawFileNode rawFileNode, string destinationPath)
        {
            if (rawFileNode.RawFileBytes == null || rawFileNode.RawFileBytes.Length == 0)
                throw new InvalidOperationException($"Raw file '{rawFileNode.FileName}' contains no data.");

            File.WriteAllBytes(destinationPath, rawFileNode.RawFileBytes);
        }
    }
}
