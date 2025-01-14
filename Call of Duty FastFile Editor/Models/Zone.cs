using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Call_of_Duty_FastFile_Editor.Models
{    
    public class Zone
    {
        /// <summary>
        /// Binary data of the zone file
        /// </summary>
        public byte[] FileData { get; set; }
        public uint ZoneFileSize { get; set; }
        public uint Unknown1 { get; set; }
        public uint Unknown2 { get; set; }
        public uint Unknown3 { get; set; }
        public uint Unknown4 { get; set; }
        public uint Unknown5 { get; set; }
        public uint Unknown6 { get; set; }
        public uint Unknown7 { get; set; }
        public uint Unknown8 { get; set; }
        public uint TagCount { get; set; }
        public uint Unknown9 { get; set; }
        public uint RecordCount { get; set; }
        public uint Unknown10 { get; set; }
        public uint Unknown11 { get; set; }
        public List<uint> TagPtrs { get; set; } = new List<uint>();
        public List<string> Tags { get; set; } = new List<string>();

        public Dictionary<string, uint> DecimalValues { get; private set; }

        /// <summary>
        /// Sets the values of locations in the zone based off the offsets from Constants
        /// Rename maybe?
        /// </summary>
        public void SetZoneOffsets()
        {
            this.ZoneFileSize = ReadUInt32AtOffset(Constants.ZoneFile.ZoneSizeOffset);
            this.Unknown1 = ReadUInt32AtOffset(Constants.ZoneFile.Unknown1Offset);
            this.Unknown2 = ReadUInt32AtOffset(Constants.ZoneFile.Unknown2Offset);
            this.Unknown3 = ReadUInt32AtOffset(Constants.ZoneFile.Unknown3Offset);
            this.Unknown4 = ReadUInt32AtOffset(Constants.ZoneFile.Unknown4Offset);
            this.Unknown5 = ReadUInt32AtOffset(Constants.ZoneFile.Unknown5Offset);
            this.Unknown6 = ReadUInt32AtOffset(Constants.ZoneFile.Unknown6Offset);
            this.Unknown7 = ReadUInt32AtOffset(Constants.ZoneFile.Unknown7Offset);
            this.Unknown8 = ReadUInt32AtOffset(Constants.ZoneFile.Unknown8Offset);
            //this.TagCount = ReadUInt32AtOffset(Constants.ZoneFile.TagCountOffset);
            this.Unknown9 = ReadUInt32AtOffset(Constants.ZoneFile.Unknown9Offset);
            //this.RecordCount = ReadUInt32AtOffset(Constants.ZoneFile.RecordCountOffset);
            this.Unknown10 = ReadUInt32AtOffset(Constants.ZoneFile.Unknown10Offset);
            this.Unknown11 = ReadUInt32AtOffset(Constants.ZoneFile.Unknown11Offset);

            SetDecimalValues();
        }

        private void SetDecimalValues()
        {
            this.DecimalValues = new Dictionary<string, uint>()
            {
                { "ZoneFileSize", ZoneFileSize },
                { "Unknown1", Unknown1 },
                { "Unknown2", Unknown2 },
                { "Unknown3", Unknown3 },
                { "Unknown4", Unknown4 },
                { "Unknown5", Unknown5 },
                { "Unknown6", Unknown6 },
                { "Unknown7", Unknown7 },
                { "Unknown8", Unknown8 },
                { "TagCount", TagCount },
                { "Unknown9", Unknown9 },
                { "RecordCount", RecordCount },
                { "Unknown10", Unknown10 },
                { "Unknown11", Unknown11 }
            };
        }

        /// <summary>
        /// Provides a formatted string of all relevant properties with their decimal values
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"ZoneFileSize: {ZoneFileSize}");
            sb.AppendLine($"Unknown1: {Unknown1}");
            sb.AppendLine($"Unknown2: {Unknown2}");
            sb.AppendLine($"Unknown3: {Unknown3}");
            sb.AppendLine($"Unknown4: {Unknown4}");
            sb.AppendLine($"Unknown5: {Unknown5}");
            sb.AppendLine($"Unknown6: {Unknown6}");
            sb.AppendLine($"Unknown7: {Unknown7}");
            sb.AppendLine($"Unknown8: {Unknown8}");
            //sb.AppendLine($"TagCount: {TagCount}");
            sb.AppendLine($"Unknown9: {Unknown9}");
            //sb.AppendLine($"RecordCount: {RecordCount}");
            sb.AppendLine($"Unknown10: {Unknown10}");
            sb.AppendLine($"Unknown11: {Unknown11}");

            return sb.ToString();
        }

        /// <summary>
        /// Helper method to read a UInt32 at a specific offset. Can specify endianness.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="isBigEndian"></param>
        /// <returns></returns>
        /// <exception cref="EndOfStreamException"></exception>
        public uint ReadUInt32AtOffset(int offset, bool isBigEndian = true)
        {
            if (offset + 4 > FileData.Length)
                throw new EndOfStreamException($"Cannot read UInt32 at offset 0x{offset:X}, exceeds file length.");

            byte[] bytes = new byte[4];
            Array.Copy(FileData, offset, bytes, 0, 4);

            if (isBigEndian)
                Array.Reverse(bytes);

            return BitConverter.ToUInt32(bytes, 0);
        }


        /// <summary>
        /// Helper method to read a null-terminated string at a specific offset
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        /// <exception cref="EndOfStreamException"></exception>
        public string ReadStringAtOffset(int offset)
        {
            if (offset >= FileData.Length)
                throw new EndOfStreamException($"Cannot read string at offset 0x{offset:X}, exceeds file length.");

            int end = offset;
            while (end < FileData.Length && FileData[end] != 0x00)
            {
                end++;
            }

            if (end == FileData.Length)
                throw new EndOfStreamException($"String starting at offset 0x{offset:X} is not null-terminated.");

            return Encoding.UTF8.GetString(FileData, offset, end - offset);
        }

        /// <summary>
        /// Helper method to get bytes at a specific offset and length
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        /// <exception cref="EndOfStreamException"></exception>
        public byte[] GetBytesAtOffset(int offset, int length = 4) // default 4 bytes aka a word
        {
            if (offset + length > FileData.Length)
                throw new EndOfStreamException($"Cannot read {length} bytes at offset 0x{offset:X}, exceeds file length.");

            byte[] bytes = new byte[length];
            Array.Copy(FileData, offset, bytes, 0, length);
            return bytes;
        }

        /// <summary>
        /// Helper method to read a string until a null terminator starting at a specific offset
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private string ReadNullTerminatedString(BinaryReader reader)
        {
            List<byte> bytes = new List<byte>();
            try
            {
                byte b;
                while ((b = reader.ReadByte()) != 0)
                {
                    bytes.Add(b);
                }
            }
            catch (EndOfStreamException)
            {
                MessageBox.Show("Unexpected end of stream while reading a null-terminated string.", "Deserialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
            return Encoding.UTF8.GetString(bytes.ToArray());
        }
    }
}
