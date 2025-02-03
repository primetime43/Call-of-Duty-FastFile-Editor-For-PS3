using Call_of_Duty_FastFile_Editor.Services;
using System.Text;

namespace Call_of_Duty_FastFile_Editor.Models
{
    public class StringTable
    {
        public int RowCount { get; set; }
        public int ColumnCount { get; set; }
        public required string TableName { get; set; }
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
        public static List<StringTable> FindCsvStringTables(Zone zone)
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
