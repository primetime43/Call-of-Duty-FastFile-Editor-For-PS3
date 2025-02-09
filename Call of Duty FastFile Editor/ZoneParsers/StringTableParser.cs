using Call_of_Duty_FastFile_Editor.Models;
using Call_of_Duty_FastFile_Editor.Services;
using System;
using System.Diagnostics;

namespace Call_of_Duty_FastFile_Editor.ZoneParsers
{
    public class StringTableParser
    {
        /// <summary>
        /// Parses a StringTable from the zone file starting at the given offset.
        /// Assumes the following structure:
        /// [ marker (4 bytes, should be 0xFFFFFFFF) ]
        /// [ columnCount (4 bytes, big-endian) ]
        /// [ rowCount (4 bytes, big-endian) ]
        /// [ valuesPointer (4 bytes, big-endian) ]
        /// [ null-terminated table name string ]
        /// </summary>
        public static StringTable ParseStringTable(FastFile openedFastFile, int startingOffset)
        {
            byte[] fileData = openedFastFile.OpenedFastFileZone.ZoneFileData;

            // Read the marker at startingOffset.
            int marker = (int)Utilities.ReadUInt32BigEndian(fileData, startingOffset);
            if (marker != -1)
            {
                Debug.WriteLine($"Unexpected marker at offset 0x{startingOffset:X}: 0x{marker:X}. Stopping extraction.");
                return null; // or throw an exception
            }

            // The next 4 bytes (at offset + 4) represent the column count.
            int columnCount = (int)Utilities.ReadUInt32BigEndian(fileData, startingOffset + 4);

            // The next 4 bytes (at offset + 8) represent the row count.
            int rowCount = (int)Utilities.ReadUInt32BigEndian(fileData, startingOffset + 8);

            // The next 4 bytes (at offset + 12) are the values pointer.
            int valuesPointer = (int)Utilities.ReadUInt32BigEndian(fileData, startingOffset + 12);

            // The inline table name starts at offset + 16.
            string inlineName = Utilities.ReadNullTerminatedString(fileData, startingOffset + 16);

            // Build the StringTable object.
            StringTable stringTable = new StringTable
            {
                ColumnCount = columnCount,
                ColumnCountOffset = startingOffset + 4,
                RowCount = rowCount,
                RowCountOffset = startingOffset + 8,
                TableName = inlineName,
                TableNameOffset = startingOffset + 16,
                StartOfFileHeader = startingOffset,
                EndOfFileHeader = startingOffset + 16 + inlineName.Length - 1,
            };

            return stringTable;
        }
    }
}
