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
        public int CodeStartPosition { get; set; }

        public int StartOfFileHeader { get; set; }
        public int EndOfFileHeader { get; set; }

        // All cell values stored in row-major order
        public string[]? Values { get; set; }

        /// <summary>
        /// Gets the string at the specified row and column.
        /// </summary>
        public string GetEntry(int row, int column)
        {
            if (Values == null)
                throw new InvalidOperationException("Table values have not been loaded.");

            if (row < 0 || row >= RowCount)
                throw new ArgumentOutOfRangeException(nameof(row), "Row is out of range.");
            if (column < 0 || column >= ColumnCount)
                throw new ArgumentOutOfRangeException(nameof(column), "Column is out of range.");

            int index = (ColumnCount * row) + column;
            return Values[index];
        }

        public void UpdateAssetRecord(ref ZoneAssetRecord assetRecord)
        {
            // For a string table, you might only want to update some fields:
            assetRecord.Name = this.TableName;
            // Optionally, you might store the total number of cells as a size, or leave it 0.
            assetRecord.Size = this.RowCount * this.ColumnCount;
            // For content, you could join the table values (if loaded) or simply leave it empty.
            assetRecord.Content = Values != null ? string.Join(", ", Values) : string.Empty;

            assetRecord.HeaderStartOffset = this.StartOfFileHeader;
            assetRecord.HeaderEndOffset = this.EndOfFileHeader;

            // If you have header offset information in your zone for string tables,
            // you can update HeaderStartOffset, etc., here as needed.
            // For now, we set them to 0.
            assetRecord.AssetDataStartPosition = 0;
            assetRecord.AssetDataEndOffset = 0;
        }
    }

    public static class StringTableOperations
    {
        /// <summary>
        /// Scans the zone file for string table entries that look like:
        /// 
        ///   [ rowCount (4 bytes, big-endian) ]
        ///   [ columnCount (4 bytes, big-endian) ]
        ///   [ 0xFF FF FF FF ]
        ///   [ null-terminated ASCII filename ending in .csv ]
        ///
        /// This is more robust than scanning for ".csv" first.
        /// </summary>
        public static List<StringTable> FindCsvStringTablesWithPattern(Zone zone)
        {
            byte[] zoneBytes = zone.ZoneFileData;
            List<StringTable> tables = new List<StringTable>();

            // We'll scan for 0xFF FF FF FF
            for (int i = 0; i < zoneBytes.Length - 4; i++)
            {
                // Check if these 4 bytes are FF FF FF FF
                if (zoneBytes[i] == 0xFF &&
                    zoneBytes[i + 1] == 0xFF &&
                    zoneBytes[i + 2] == 0xFF &&
                    zoneBytes[i + 3] == 0xFF)
                {
                    int pointerIndex = i;

                    // We expect RowCount at (pointerIndex - 8) and ColumnCount at (pointerIndex - 4).
                    // Make sure we don't go negative:
                    int rowCountOffset = pointerIndex - 8;
                    int colCountOffset = pointerIndex - 4;
                    if (rowCountOffset < 0 || colCountOffset < 0)
                        continue;

                    // Read rowCount/columnCount in big-endian
                    uint rowCountU = Utilities.ReadUInt32AtOffset(rowCountOffset, zone, isBigEndian: true);
                    uint columnCountU = Utilities.ReadUInt32AtOffset(colCountOffset, zone, isBigEndian: true);

                    // Now read the name from pointerIndex+4 (null-terminated)
                    int nameStart = pointerIndex + 4;
                    if (nameStart >= zoneBytes.Length)
                        continue;

                    string tableName = Utilities.ReadStringAtOffset(nameStart, zone);

                    // Make sure the table name ends with .csv, and has at least one slash in it
                    // (Based on your requirement that we see strings like "mp/statsTable.csv", etc.)
                    bool looksLikeCsv = tableName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
                                        && tableName.Contains('/');

                    if (looksLikeCsv)
                    {
                        tables.Add(new StringTable
                        {
                            RowCount = (int)rowCountU,
                            ColumnCount = (int)columnCountU,
                            TableName = tableName
                        });
                    }
                }
            }

            return tables;
        }
    }
}
