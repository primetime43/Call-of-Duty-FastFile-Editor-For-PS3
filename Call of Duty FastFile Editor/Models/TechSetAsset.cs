namespace Call_of_Duty_FastFile_Editor.Models
{
    /// <summary>
    /// Represents a MaterialTechniqueSet asset from the zone file.
    /// TechniqueSets define shader rendering passes for materials.
    ///
    /// Structure (PC/PS3):
    /// struct MaterialTechniqueSet {
    ///     const char* name;                          // 4 bytes - pointer (0xFFFFFFFF when inline)
    ///     char worldVertFormat;                      // 1 byte
    ///     char hasBeenUploaded;                      // 1 byte
    ///     char unused[2];                            // 2 bytes padding
    ///     MaterialTechniqueSet* remappedTechniqueSet;// 4 bytes - pointer (usually NULL or 0xFFFFFFFF)
    ///     MaterialTechnique* techniques[34];         // 136 bytes - array of 34 technique pointers
    /// };
    /// Total header: 148 bytes
    /// </summary>
    public class TechSetAsset : IAssetRecordUpdatable
    {
        /// <summary>
        /// Total number of technique slots in a TechniqueSet.
        /// PS3: CoD4=26, WaW=51, MW2=37
        /// PC:  CoD4=34, WaW=59, MW2=48
        /// </summary>
        public const int TECHNIQUE_COUNT_PS3_WAW = 51;
        public const int TECHNIQUE_COUNT = TECHNIQUE_COUNT_PS3_WAW; // Default to PS3 WaW

        /// <summary>
        /// Size of the MaterialTechniqueSet header in bytes.
        /// Structure: name ptr (4) + worldVertFormat (1) + techniques[51] (204) = 209 bytes for PS3 WaW
        /// </summary>
        public const int HEADER_SIZE = 4 + 1 + (TECHNIQUE_COUNT * 4); // 209 bytes for PS3 WaW

        /// <summary>
        /// Name of the technique set.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// World vertex format value (0x00 to 0x0B typically).
        /// </summary>
        public byte WorldVertFormat { get; set; }

        /// <summary>
        /// Whether the techset has been uploaded to GPU.
        /// </summary>
        public bool HasBeenUploaded { get; set; }

        /// <summary>
        /// Number of active techniques in this set (non-null pointers).
        /// </summary>
        public int ActiveTechniqueCount { get; set; }

        /// <summary>
        /// Array of technique info for each slot.
        /// Index corresponds to technique type (e.g., 0=depth, 1=build shadowmap, etc.)
        /// </summary>
        public TechniqueInfo[] Techniques { get; set; } = new TechniqueInfo[TECHNIQUE_COUNT];

        /// <summary>
        /// Offset where the asset header starts in the zone file.
        /// </summary>
        public int StartOffset { get; set; }

        /// <summary>
        /// Implements IAssetRecordUpdatable.StartOfFileHeader.
        /// </summary>
        public int StartOfFileHeader
        {
            get => StartOffset;
            set => StartOffset = value;
        }

        /// <summary>
        /// Offset where the asset data ends in the zone file.
        /// </summary>
        public int EndOffset { get; set; }

        /// <summary>
        /// Implements IAssetRecordUpdatable.EndOfFileHeader.
        /// </summary>
        public int EndOfFileHeader => EndOffset;

        /// <summary>
        /// Additional parsing information.
        /// </summary>
        public string AdditionalData { get; set; } = string.Empty;

        public void UpdateAssetRecord(ref ZoneAssetRecord record)
        {
            record.Name = Name;
            record.AssetRecordEndOffset = EndOffset;
            record.Content = $"Techniques: {ActiveTechniqueCount}/{TECHNIQUE_COUNT}, VertFormat: 0x{WorldVertFormat:X2}";
        }
    }

    /// <summary>
    /// Information about a single technique within a TechniqueSet.
    /// </summary>
    public class TechniqueInfo
    {
        /// <summary>
        /// Whether this technique slot has data (pointer was non-null).
        /// </summary>
        public bool IsPresent { get; set; }

        /// <summary>
        /// Name of the technique.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Technique flags.
        /// </summary>
        public ushort Flags { get; set; }

        /// <summary>
        /// Number of passes in this technique.
        /// </summary>
        public ushort PassCount { get; set; }

        /// <summary>
        /// Offset where this technique starts in the zone.
        /// </summary>
        public int StartOffset { get; set; }

        /// <summary>
        /// Offset where this technique ends in the zone.
        /// </summary>
        public int EndOffset { get; set; }
    }
}
