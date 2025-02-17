using Call_of_Duty_FastFile_Editor.Services;
using System.Text;

namespace Call_of_Duty_FastFile_Editor.Models
{
    public class StringTable : IAssetRecordUpdatable
    {
        public StringTable()
        {

        }

        public int RowCount { get; set; }
        public int RowCountOffset { get; set; }
        public int ColumnCount { get; set; }
        public int ColumnCountOffset { get; set; }
        public required string TableName { get; set; }
        public int TableNameOffset { get; set; }
        public int DataStartPosition { get; set; }
        public int DataEndPosition { get; set; }

        public int StartOfFileHeader { get; set; }
        public int EndOfFileHeader { get; set; }


        // Each item is (Offset, Text).
        public List<(int Offset, string Text)>? Cells { get; set; }

        public string AdditionalData { get; set; }

        public void UpdateAssetRecord(ref ZoneAssetRecord assetRecord)
        {
            assetRecord.Name = this.TableName;

            // Use the new Cells list to build a single string of all cell text
            if (Cells != null)
            {
                // If you only want the text:
                assetRecord.Content = string.Join(", ", Cells.Select(c => c.Text));

                // Alternatively, if you want offsets too (in hex):
                // assetRecord.Content = string.Join("; ", Cells.Select(c => $"0x{c.Offset:X}: {c.Text}"));
            }
            else
            {
                assetRecord.Content = string.Empty;
            }

            assetRecord.HeaderStartOffset = this.StartOfFileHeader;
            assetRecord.HeaderEndOffset = this.EndOfFileHeader;
            assetRecord.AdditionalData = this.AdditionalData;
            assetRecord.AssetDataStartPosition = this.DataStartPosition;
            assetRecord.AssetDataEndOffset = this.DataEndPosition;
            assetRecord.AssetRecordEndOffset = this.DataEndPosition + 1;
        }

        /// <summary>
        /// Scans the zone file for string table entries that look like:
        /// 
        ///   [ rowCount (4 bytes, big-endian) ]
        ///   [ columnCount (4 bytes, big-endian) ]
        ///   [ 0xFF FF FF FF ]
        ///   [ null-terminated ASCII filename ending in .csv ]
        ///
        /// This is more robust than scanning for ".csv" first.
        /// TEMP EVENTUALLY DELETE
        /// </summary>
        public static StringTable? FindSingleCsvStringTableWithPattern(Zone zone, int startingOffset)
        {
            byte[] zoneBytes = zone.ZoneFileData;

            // Validate the starting offset
            if (startingOffset < 0 || startingOffset >= zoneBytes.Length)
                throw new ArgumentOutOfRangeException(nameof(startingOffset), "Starting offset is outside the zone file data range.");

            // Start scanning from the provided offset.
            for (int i = startingOffset; i < zoneBytes.Length - 4; i++)
            {
                // Check if these 4 bytes are FF FF FF FF
                if (zoneBytes[i] == 0xFF &&
                    zoneBytes[i + 1] == 0xFF &&
                    zoneBytes[i + 2] == 0xFF &&
                    zoneBytes[i + 3] == 0xFF)
                {
                    int pointerIndex = i;

                    // We expect RowCount at (pointerIndex - 8) and ColumnCount at (pointerIndex - 4).
                    int rowCountOffset = pointerIndex - 8;
                    int colCountOffset = pointerIndex - 4;
                    if (rowCountOffset < 0 || colCountOffset < 0)
                        continue;

                    // Read rowCount and columnCount in big-endian
                    uint rowCountU = Utilities.ReadUInt32AtOffset(rowCountOffset, zone, isBigEndian: true);
                    uint columnCountU = Utilities.ReadUInt32AtOffset(colCountOffset, zone, isBigEndian: true);

                    // Now read the table name from pointerIndex + 4 (null-terminated)
                    int nameStart = pointerIndex + 4;
                    if (nameStart >= zoneBytes.Length)
                        continue;

                    string tableName = Utilities.ReadStringAtOffset(nameStart, zone);

                    // Validate that the table name ends with .csv and contains at least one slash.
                    bool looksLikeCsv = tableName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
                                        && tableName.Contains('/');

                    if (looksLikeCsv)
                    {
                        return new StringTable
                        {
                            RowCount = (int)rowCountU,
                            ColumnCount = (int)columnCountU,
                            TableName = tableName
                            // Optionally, you might store the offsets if needed:
                            // RowCountOffset = rowCountOffset,
                            // ColumnCountOffset = colCountOffset,
                            // TableNameOffset = nameStart,
                        };
                    }
                }
            }

            // If no valid string table is found, return null.
            return null;
        }
    }
}
