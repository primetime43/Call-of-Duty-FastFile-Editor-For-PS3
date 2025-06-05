using Call_of_Duty_FastFile_Editor.Services;
using Call_of_Duty_FastFile_Editor.ZoneParsers;
using System.Net;
using System.Text;
using Call_of_Duty_FastFile_Editor.Constants;

namespace Call_of_Duty_FastFile_Editor.Models
{
    public class ZoneFile
    {
        public FastFile ParentFastFile { get; set; }

        /// <summary>The full path to the .zone file.</summary>
        public string FilePath { get; private set; }

        /// <summary>All bytes of the .zone file.</summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// Constructs the wrapper; actual loading is done in Load().
        /// </summary>
        public ZoneFile(string path, FastFile currentFF)
        {
            FilePath = path ?? throw new ArgumentNullException(nameof(path));
            ParentFastFile = currentFF ?? throw new ArgumentNullException(nameof(currentFF));
        }

        /// <summary>
        /// Creates a ZoneFile, loads its bytes, and reads its header fields.
        /// </summary>
        public static ZoneFile Load(string path, FastFile fastFile)
        {
            if (fastFile == null)
                throw new ArgumentNullException(nameof(fastFile));

            var z = new ZoneFile(path, fastFile);
            z.LoadData();
            z.ReadHeaderFields();
            return z;
        }

        /// <summary>Modify on-disk file, then refresh Data.</summary>
        public void ModifyZoneFile(Action<FileStream> modification)
        {
            using (FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                modification(fs);
            }
            LoadData();
        }

        // Various zone header properties.
        public uint FileSize { get; private set; }
        public uint Unknown1 { get; private set; }
        public uint Unknown2 { get; private set; }
        public uint Unknown3 { get; private set; }
        public uint Unknown4 { get; private set; }
        public uint Unknown5 { get; private set; }
        public uint EndOfFileDataPointer { get; private set; }
        public uint Unknown7 { get; private set; }
        public uint Unknown8 { get; private set; }
        public uint TagCount { get; private set; }
        public uint Unknown10 { get; private set; }
        public uint AssetRecordCount { get; private set; }

        // For display or debugging purposes.
        public Dictionary<string, uint>? HeaderFieldValues { get; private set; }

        // The asset mapping container.
        public ZoneFileAssetManifest ZoneFileAssets { get; set; } = new ZoneFileAssetManifest();

        public int AssetPoolStartOffset { get; internal set; }
        public int AssetPoolEndOffset { get; internal set; }

        // Mapping of property names to their offsets (pulled from the Constants).
        private readonly Dictionary<string, int> _headerFieldOffsets = new Dictionary<string, int>
        {
            { "FileSize", ZoneFileHeaderConstants.ZoneSizeOffset },
            { "Unknown1", ZoneFileHeaderConstants.Unknown1Offset },
            { "Unknown2", ZoneFileHeaderConstants.Unknown2Offset },
            { "Unknown3", ZoneFileHeaderConstants.Unknown3Offset },
            { "Unknown4", ZoneFileHeaderConstants.Unknown4Offset },
            { "Unknown5", ZoneFileHeaderConstants.Unknown5Offset },
            { "EndOfFileDataPointer", ZoneFileHeaderConstants.EndOfFileDataPointer }, // end of data is FileSize + 36 bytes?
            { "Unknown7", ZoneFileHeaderConstants.Unknown7Offset },
            { "Unknown8", ZoneFileHeaderConstants.Unknown8Offset },
            { "TagCount", ZoneFileHeaderConstants.TagCountOffset },
            { "Unknown10", ZoneFileHeaderConstants.Unknown10Offset },
            { "AssetRecordCount", ZoneFileHeaderConstants.AssetRecordCountOffset }
        };

        /// <summary>Reloads Data from disk.</summary>
        public void LoadData() => Data = File.ReadAllBytes(FilePath);

        /// <summary>Parses the zone’s asset pool into ZoneFileAssets & offsets.</summary>
        public void ParseAssetPool()
        {
            var parser = new AssetPoolParser(this);
            parser.MapZoneAssetsPoolAndGetEndOffset();
        }

        /// <summary>
        /// For UI: “0x…” hex offset of any header field.
        /// </summary>
        public string GetZoneOffset(string zoneName)
        {
            if (_headerFieldOffsets.TryGetValue(zoneName, out int offset))
            {
                return $"0x{offset:X2}";
            }
            else
            {
                return "N/A";
            }
        }

        /// <summary>
        /// Reads every header field into HeaderFieldValues and populates the strongly‑typed props.
        /// </summary>
        public void ReadHeaderFields()
        {
            // Read every header field into a Dictionary<string,uint>
            HeaderFieldValues = _headerFieldOffsets
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => ReadField(kvp.Key)
                );

            // Populate each strongly‑typed property from that Dictionary
            FileSize = HeaderFieldValues[nameof(FileSize)];
            Unknown1 = HeaderFieldValues[nameof(Unknown1)];
            Unknown2 = HeaderFieldValues[nameof(Unknown2)];
            Unknown3 = HeaderFieldValues[nameof(Unknown3)];
            Unknown4 = HeaderFieldValues[nameof(Unknown4)];
            Unknown5 = HeaderFieldValues[nameof(Unknown5)];
            EndOfFileDataPointer = HeaderFieldValues[nameof(EndOfFileDataPointer)];
            Unknown7 = HeaderFieldValues[nameof(Unknown7)];
            Unknown8 = HeaderFieldValues[nameof(Unknown8)];
            TagCount = HeaderFieldValues[nameof(TagCount)];
            Unknown10 = HeaderFieldValues[nameof(Unknown10)];
            AssetRecordCount = HeaderFieldValues[nameof(AssetRecordCount)];
        }

        /// <summary>
        /// Helper that looks up the offset for a header‑field name and reads a BE uint from Data.
        /// </summary>
        private uint ReadField(string name)
        {
            int offset = _headerFieldOffsets[name];
            return Utilities.ReadUInt32AtOffset(offset, this);
        }
    }
}
