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
        /// Also updates the BlockSizeLarge to stay in sync (raw file data goes into the LARGE block).
        /// </summary>
        public static void WriteZoneFileSize(string path, uint newSize)
        {
            // Read the current BlockSizeLarge to calculate the offset from ZoneSize
            uint currentZoneSize = ReadZoneFileSize(path);
            uint currentBlockSizeLarge = ReadBlockSizeLarge(path);

            // Calculate the difference between BlockSizeLarge and ZoneSize
            // This offset should remain constant when we update the size
            uint blockOffset = currentBlockSizeLarge - currentZoneSize;
            uint newBlockSizeLarge = newSize + blockOffset;

            Span<byte> b = stackalloc byte[4];
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Write);

            // Write the new ZoneSize
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(b, newSize);
            fs.Seek(ZoneFileHeaderConstants.ZoneSizeOffset, SeekOrigin.Begin);
            fs.Write(b);

            // Write the new BlockSizeLarge
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(b, newBlockSizeLarge);
            fs.Seek(ZoneFileHeaderConstants.BlockSizeLargeOffset, SeekOrigin.Begin);
            fs.Write(b);
        }

        /// <summary>
        /// Reads the BlockSizeLarge from the zone header (big-endian).
        /// This represents the XFILE_BLOCK_LARGE allocation size.
        /// </summary>
        public static uint ReadBlockSizeLarge(string path)
        {
            var b = new byte[4];
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            fs.Seek(ZoneFileHeaderConstants.BlockSizeLargeOffset, SeekOrigin.Begin);
            fs.Read(b, 0, 4);
            return System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(b);
        }
    }
}
