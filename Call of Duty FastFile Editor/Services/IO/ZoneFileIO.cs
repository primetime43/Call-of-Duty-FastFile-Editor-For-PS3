using Call_of_Duty_FastFile_Editor.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Call_of_Duty_FastFile_Editor.Services.IO
{
    public class ZoneFileIO
    {
        /// <summary>
        /// Reads the 4-byte zone file size from the header (big-endian) at the defined offset.
        /// </summary>
        public static uint ReadZoneFileSize(string path)
        {
            var b = new byte[4];
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            fs.Seek(ZoneFileHeaderConstants.ZoneSizeOffset, SeekOrigin.Begin);
            fs.Read(b, 0, 4);
            return System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(b);
        }

        /// <summary>
        /// Writes the updated zone file size (big-endian) to the header at the defined offset.
        /// Also updates the EndOfFileDataPointer to stay in sync.
        /// </summary>
        public static void WriteZoneFileSize(string path, uint newSize)
        {
            // Read the current EndOfFileDataPointer to calculate the offset from FileSize
            uint currentFileSize = ReadZoneFileSize(path);
            uint currentEndPointer = ReadEndOfFileDataPointer(path);

            // Calculate the difference between EndOfFileDataPointer and FileSize
            // This offset should remain constant when we update the size
            uint pointerOffset = currentEndPointer - currentFileSize;
            uint newEndPointer = newSize + pointerOffset;

            Span<byte> b = stackalloc byte[4];
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Write);

            // Write the new FileSize
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(b, newSize);
            fs.Seek(ZoneFileHeaderConstants.ZoneSizeOffset, SeekOrigin.Begin);
            fs.Write(b);

            // Write the new EndOfFileDataPointer
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(b, newEndPointer);
            fs.Seek(ZoneFileHeaderConstants.EndOfFileDataPointer, SeekOrigin.Begin);
            fs.Write(b);
        }

        /// <summary>
        /// Reads the EndOfFileDataPointer from the zone header (big-endian).
        /// </summary>
        public static uint ReadEndOfFileDataPointer(string path)
        {
            var b = new byte[4];
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            fs.Seek(ZoneFileHeaderConstants.EndOfFileDataPointer, SeekOrigin.Begin);
            fs.Read(b, 0, 4);
            return System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(b);
        }
    }
}
