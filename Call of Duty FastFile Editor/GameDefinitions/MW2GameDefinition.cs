using Call_of_Duty_FastFile_Editor.Models;
using System.Diagnostics;
using System.Text;

namespace Call_of_Duty_FastFile_Editor.GameDefinitions
{
    /// <summary>
    /// Game definition implementation for Call of Duty: Modern Warfare 2.
    /// MW2 may have different rawfile structure compared to CoD4/CoD5.
    /// </summary>
    public class MW2GameDefinition : GameDefinitionBase
    {
        public override string GameName => MW2Definition.GameName;
        public override string ShortName => "MW2";
        public override int VersionValue => MW2Definition.VersionValue;
        public override int PCVersionValue => MW2Definition.PCVersionValue;
        public override byte[] VersionBytes => MW2Definition.VersionBytes;
        public override byte RawFileAssetType => MW2Definition.RawFileAssetType;
        public override byte LocalizeAssetType => MW2Definition.LocalizeAssetType;

        public override string GetAssetTypeName(int assetType)
        {
            if (Enum.IsDefined(typeof(MW2AssetType), assetType))
            {
                return ((MW2AssetType)assetType).ToString();
            }
            return $"unknown_0x{assetType:X2}";
        }

        public override bool IsSupportedAssetType(int assetType)
        {
            return assetType == RawFileAssetType || assetType == LocalizeAssetType;
        }

        /// <summary>
        /// MW2 rawfile parsing - tries multiple structure formats.
        /// MW2 PS3 rawfile structure may differ from CoD4/CoD5.
        ///
        /// Known MW2 rawfile structures:
        /// Format 1 (Standard): [FF FF FF FF] [4-byte size BE] [FF FF FF FF] [name\0] [data]
        /// Format 2 (Alternate): [FF FF FF FF] [FF FF FF FF] [4-byte size BE] [name\0] [data]
        /// </summary>
        public override RawFileNode? ParseRawFile(byte[] zoneData, int offset)
        {
            Debug.WriteLine($"[MW2] ParseRawFile at offset 0x{offset:X}");

            // Try standard format first (same as CoD4/CoD5)
            var result = TryParseStandardFormat(zoneData, offset);
            if (result != null)
            {
                Debug.WriteLine($"[MW2] Parsed using standard format");
                return result;
            }

            // Try MW2-specific alternate format
            result = TryParseAlternateFormat(zoneData, offset);
            if (result != null)
            {
                Debug.WriteLine($"[MW2] Parsed using alternate format");
                return result;
            }

            // Try format with pointer marker first
            result = TryParsePointerFirstFormat(zoneData, offset);
            if (result != null)
            {
                Debug.WriteLine($"[MW2] Parsed using pointer-first format");
                return result;
            }

            Debug.WriteLine($"[MW2] Failed to parse rawfile at 0x{offset:X}");
            return null;
        }

        /// <summary>
        /// Standard format: [FF FF FF FF] [4-byte size BE] [FF FF FF FF] [name\0] [data]
        /// </summary>
        private RawFileNode? TryParseStandardFormat(byte[] zoneData, int offset)
        {
            if (offset + 12 > zoneData.Length) return null;

            uint marker1 = ReadUInt32BE(zoneData, offset);
            if (marker1 != 0xFFFFFFFF) return null;

            int dataLength = ReadInt32BE(zoneData, offset + 4);
            if (dataLength <= 0 || dataLength > zoneData.Length) return null;

            uint marker2 = ReadUInt32BE(zoneData, offset + 8);
            if (marker2 != 0xFFFFFFFF) return null;

            return ParseRawFileContent(zoneData, offset, 12, dataLength);
        }

        /// <summary>
        /// Alternate format: [FF FF FF FF] [FF FF FF FF] [4-byte size BE] [name\0] [data]
        /// Some MW2 zones may use double pointer markers before size.
        /// </summary>
        private RawFileNode? TryParseAlternateFormat(byte[] zoneData, int offset)
        {
            if (offset + 16 > zoneData.Length) return null;

            uint marker1 = ReadUInt32BE(zoneData, offset);
            if (marker1 != 0xFFFFFFFF) return null;

            uint marker2 = ReadUInt32BE(zoneData, offset + 4);
            if (marker2 != 0xFFFFFFFF) return null;

            int dataLength = ReadInt32BE(zoneData, offset + 8);
            if (dataLength <= 0 || dataLength > zoneData.Length) return null;

            // Check for valid filename start (printable ASCII)
            int nameOffset = offset + 12;
            if (nameOffset >= zoneData.Length) return null;
            byte firstChar = zoneData[nameOffset];
            if (firstChar < 0x20 || firstChar > 0x7E) return null;

            return ParseRawFileContent(zoneData, offset, 12, dataLength);
        }

        /// <summary>
        /// Pointer-first format for MW2: May have name pointer before size
        /// [FF FF FF FF] [name\0] [size BE] [data]
        /// </summary>
        private RawFileNode? TryParsePointerFirstFormat(byte[] zoneData, int offset)
        {
            if (offset + 8 > zoneData.Length) return null;

            uint marker1 = ReadUInt32BE(zoneData, offset);
            if (marker1 != 0xFFFFFFFF) return null;

            // Check if next bytes look like a filename (printable ASCII)
            int possibleNameStart = offset + 4;
            if (possibleNameStart >= zoneData.Length) return null;
            byte firstChar = zoneData[possibleNameStart];
            if (firstChar < 0x20 || firstChar > 0x7E) return null;

            // Try to read a filename
            string fileName = ReadNullTerminatedString(zoneData, possibleNameStart);
            if (string.IsNullOrEmpty(fileName) || fileName.Length < 3) return null;

            // Check if it looks like a valid file path
            if (!fileName.Contains('/') && !fileName.Contains('.')) return null;

            int nameByteLen = Encoding.UTF8.GetByteCount(fileName) + 1;
            int sizeOffset = possibleNameStart + nameByteLen;

            if (sizeOffset + 4 > zoneData.Length) return null;

            int dataLength = ReadInt32BE(zoneData, sizeOffset);
            if (dataLength <= 0 || dataLength > zoneData.Length) return null;

            int dataOffset = sizeOffset + 4;

            var node = new RawFileNode
            {
                StartOfFileHeader = offset,
                MaxSize = dataLength,
                FileName = fileName
            };

            if (dataOffset + dataLength <= zoneData.Length)
            {
                byte[] rawBytes = new byte[dataLength];
                Array.Copy(zoneData, dataOffset, rawBytes, 0, dataLength);
                node.RawFileBytes = rawBytes;
                node.RawFileContent = Encoding.UTF8.GetString(rawBytes);
                // Always add +1 for null terminator (consistent with old computed property)
                node.RawFileEndPosition = dataOffset + dataLength + 1;
            }

            return node;
        }

        /// <summary>
        /// Helper to parse the actual content once header structure is determined.
        /// </summary>
        private RawFileNode? ParseRawFileContent(byte[] zoneData, int headerOffset, int headerSize, int dataLength)
        {
            int fileNameOffset = headerOffset + headerSize;
            if (fileNameOffset >= zoneData.Length) return null;

            string fileName = ReadNullTerminatedString(zoneData, fileNameOffset);
            if (string.IsNullOrEmpty(fileName)) return null;

            var node = new RawFileNode
            {
                StartOfFileHeader = headerOffset,
                MaxSize = dataLength,
                FileName = fileName
            };

            int nameByteCount = Encoding.UTF8.GetByteCount(fileName) + 1;
            int fileDataOffset = fileNameOffset + nameByteCount;

            if (fileDataOffset + dataLength <= zoneData.Length)
            {
                byte[] rawBytes = new byte[dataLength];
                Array.Copy(zoneData, fileDataOffset, rawBytes, 0, dataLength);
                node.RawFileBytes = rawBytes;
                node.RawFileContent = Encoding.UTF8.GetString(rawBytes);
                // Always add +1 for null terminator (consistent with old computed property)
                node.RawFileEndPosition = fileDataOffset + dataLength + 1;
            }
            else
            {
                node.RawFileBytes = Array.Empty<byte>();
                node.RawFileContent = string.Empty;
            }

            Debug.WriteLine($"[MW2] Found rawfile: '{fileName}' size={dataLength}");
            return node;
        }
    }
}
