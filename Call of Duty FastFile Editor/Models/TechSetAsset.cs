namespace Call_of_Duty_FastFile_Editor.Models
{
    /// <summary>
    /// Represents a TechniqueSet asset from the zone file.
    /// TechniqueSets define shader rendering passes.
    /// </summary>
    public class TechSetAsset : IAssetRecordUpdatable
    {
        /// <summary>
        /// Name of the technique set.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// World vertex format value.
        /// </summary>
        public int WorldVertFormat { get; set; }

        /// <summary>
        /// Number of techniques in this set.
        /// </summary>
        public int TechniqueCount { get; set; }

        /// <summary>
        /// Offset where the asset header starts in the zone file.
        /// </summary>
        public int StartOfFileHeader { get; set; }

        /// <summary>
        /// Offset where the asset data ends in the zone file.
        /// </summary>
        public int EndOffset { get; set; }

        /// <summary>
        /// Position where the file header ends (implements IAssetRecordUpdatable).
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
            record.Content = $"Techniques: {TechniqueCount}, VertFormat: 0x{WorldVertFormat:X}";
        }
    }
}
