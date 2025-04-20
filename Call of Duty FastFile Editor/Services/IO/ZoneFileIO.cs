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
            fs.Seek(Constants.ZoneFile.ZoneSizeOffset, SeekOrigin.Begin);
            fs.Read(b, 0, 4);
            return System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(b);
        }

        /// <summary>
        /// Writes the updated zone file size (big-endian) to the header at the defined offset.
        /// </summary>
        public static void WriteZoneFileSize(string path, uint newSize)
        {
            Span<byte> b = stackalloc byte[4];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(b, newSize);
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Write);
            fs.Seek(Constants.ZoneFile.ZoneSizeOffset, SeekOrigin.Begin);
            fs.Write(b);
        }
    }
}
