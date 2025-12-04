using Call_of_Duty_FastFile_Editor.Models;
using Call_of_Duty_FastFile_Editor.Services;
using Call_of_Duty_FastFile_Editor.ZoneParsers;
using System.Diagnostics;
using System.Text;

namespace Call_of_Duty_FastFile_Editor.GameDefinitions
{
    /// <summary>
    /// Base class providing common parsing functionality for all game definitions.
    /// Game-specific implementations can override methods as needed.
    /// </summary>
    public abstract class GameDefinitionBase : IGameDefinition
    {
        public abstract string GameName { get; }
        public abstract string ShortName { get; }
        public abstract int VersionValue { get; }
        public abstract int PCVersionValue { get; }
        public abstract byte[] VersionBytes { get; }
        public abstract byte RawFileAssetType { get; }
        public abstract byte LocalizeAssetType { get; }
        public virtual byte MenuFileAssetType => 0; // Default 0 means not supported

        public virtual bool IsRawFileType(int assetType) => assetType == RawFileAssetType;
        public virtual bool IsLocalizeType(int assetType) => assetType == LocalizeAssetType;
        public virtual bool IsMenuFileType(int assetType) => MenuFileAssetType != 0 && assetType == MenuFileAssetType;
        public virtual bool IsMaterialType(int assetType) => false; // Override in game-specific definitions
        public virtual bool IsTechSetType(int assetType) => false; // Override in game-specific definitions
        public virtual bool IsSupportedAssetType(int assetType) => IsRawFileType(assetType) || IsLocalizeType(assetType) || IsMenuFileType(assetType);
        public abstract string GetAssetTypeName(int assetType);

        /// <summary>
        /// Default rawfile structure for CoD4/CoD5:
        /// [FF FF FF FF] [4-byte size BE] [FF FF FF FF] [null-terminated name] [data]
        /// </summary>
        public virtual RawFileNode? ParseRawFile(byte[] zoneData, int offset)
        {
            Debug.WriteLine($"[{ShortName}] ParseRawFile at offset 0x{offset:X}");

            // Ensure enough bytes for header (12 bytes minimum)
            if (offset > zoneData.Length - 12)
            {
                Debug.WriteLine($"[{ShortName}] Not enough bytes for header at 0x{offset:X}");
                return null;
            }

            // Read and validate first marker (should be 0xFFFFFFFF)
            uint marker1 = ReadUInt32BE(zoneData, offset);
            if (marker1 != 0xFFFFFFFF)
            {
                Debug.WriteLine($"[{ShortName}] Unexpected marker1 at 0x{offset:X}: 0x{marker1:X}");
                return null;
            }

            // Read data length (size of the file data)
            int dataLength = (int)ReadUInt32BE(zoneData, offset + 4);
            if (dataLength == 0 || dataLength > zoneData.Length)
            {
                Debug.WriteLine($"[{ShortName}] Invalid dataLength: {dataLength} at 0x{offset + 4:X}");
                return null;
            }

            // Read and validate second marker (should be 0xFFFFFFFF)
            uint marker2 = ReadUInt32BE(zoneData, offset + 8);
            if (marker2 != 0xFFFFFFFF)
            {
                Debug.WriteLine($"[{ShortName}] Unexpected marker2 at 0x{offset + 8:X}: 0x{marker2:X}");
                return null;
            }

            var node = new RawFileNode
            {
                StartOfFileHeader = offset,
                MaxSize = dataLength
            };

            // Read null-terminated filename after header
            int fileNameOffset = offset + 12;
            string fileName = ReadNullTerminatedString(zoneData, fileNameOffset);
            node.FileName = fileName;

            Debug.WriteLine($"[{ShortName}] Found rawfile: '{fileName}' size={dataLength}");

            // Calculate filename byte length including null terminator
            int nameByteCount = Encoding.UTF8.GetByteCount(fileName) + 1;
            int fileDataOffset = fileNameOffset + nameByteCount;

            // Read file data
            if (fileDataOffset + dataLength <= zoneData.Length)
            {
                byte[] rawBytes = new byte[dataLength];
                Array.Copy(zoneData, fileDataOffset, rawBytes, 0, dataLength);
                node.RawFileBytes = rawBytes;
                node.RawFileContent = Encoding.UTF8.GetString(rawBytes);

                // Calculate end position (data end + 1 for null terminator)
                // This matches the old computed property: CodeStartPosition + MaxSize + 1
                node.RawFileEndPosition = fileDataOffset + dataLength + 1;
            }
            else
            {
                Debug.WriteLine($"[{ShortName}] Data exceeds zone length");
                node.RawFileBytes = Array.Empty<byte>();
                node.RawFileContent = string.Empty;
            }

            return node;
        }

        /// <summary>
        /// Default localize structure:
        /// [FF FF FF FF FF FF FF FF] [null-terminated value] [null-terminated key]
        /// </summary>
        public virtual (LocalizedEntry? entry, int nextOffset) ParseLocalizedEntry(byte[] zoneData, int offset)
        {
            Debug.WriteLine($"[{ShortName}] ParseLocalizedEntry at offset 0x{offset:X}");

            // Check for 8-byte marker
            if (offset + 8 > zoneData.Length)
            {
                return (null, offset);
            }

            // Validate marker (FF FF FF FF FF FF FF FF)
            bool validMarker = true;
            for (int i = 0; i < 8; i++)
            {
                if (zoneData[offset + i] != 0xFF)
                {
                    validMarker = false;
                    break;
                }
            }

            if (!validMarker)
            {
                Debug.WriteLine($"[{ShortName}] Invalid localize marker at 0x{offset:X}");
                return (null, offset);
            }

            int currentOffset = offset + 8;

            // Read localized value (null-terminated)
            string localizedValue = ReadNullTerminatedString(zoneData, currentOffset);
            currentOffset += Encoding.UTF8.GetByteCount(localizedValue) + 1;

            // Read key/reference (null-terminated)
            string key = ReadNullTerminatedString(zoneData, currentOffset);
            currentOffset += Encoding.UTF8.GetByteCount(key) + 1;

            var entry = new LocalizedEntry
            {
                Key = key,
                LocalizedText = localizedValue
            };

            Debug.WriteLine($"[{ShortName}] Found localize: key='{key}'");

            return (entry, currentOffset);
        }

        /// <summary>
        /// Default menufile parsing using MenuListParser.
        /// </summary>
        public virtual MenuList? ParseMenuFile(byte[] zoneData, int offset)
        {
            Debug.WriteLine($"[{ShortName}] ParseMenuFile at offset 0x{offset:X}");
            return MenuListParser.ParseMenuList(zoneData, offset, isBigEndian: true);
        }

        /// <summary>
        /// Default material parsing using MaterialParser.
        /// </summary>
        public virtual MaterialAsset? ParseMaterial(byte[] zoneData, int offset)
        {
            Debug.WriteLine($"[{ShortName}] ParseMaterial at offset 0x{offset:X}");
            return MaterialParser.ParseMaterial(zoneData, offset, isBigEndian: true);
        }

        /// <summary>
        /// Default techset parsing using TechSetParser.
        /// </summary>
        public virtual TechSetAsset? ParseTechSet(byte[] zoneData, int offset)
        {
            Debug.WriteLine($"[{ShortName}] ParseTechSet at offset 0x{offset:X}");
            return TechSetParser.ParseTechSet(zoneData, offset, isBigEndian: true);
        }

        #region Helper Methods

        protected static uint ReadUInt32BE(byte[] data, int offset)
        {
            if (offset + 4 > data.Length) return 0;
            return (uint)((data[offset] << 24) | (data[offset + 1] << 16) |
                          (data[offset + 2] << 8) | data[offset + 3]);
        }

        protected static int ReadInt32BE(byte[] data, int offset)
        {
            return (int)ReadUInt32BE(data, offset);
        }

        protected static string ReadNullTerminatedString(byte[] data, int offset)
        {
            var sb = new StringBuilder();
            while (offset < data.Length && data[offset] != 0x00)
            {
                sb.Append((char)data[offset]);
                offset++;
            }
            return sb.ToString();
        }

        #endregion
    }
}
