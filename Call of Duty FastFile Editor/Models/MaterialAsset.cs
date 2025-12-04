namespace Call_of_Duty_FastFile_Editor.Models
{
    /// <summary>
    /// Represents a Material asset from the zone file.
    /// Materials define how surfaces are rendered, linking textures to shaders.
    /// </summary>
    public class MaterialAsset : IAssetRecordUpdatable
    {
        /// <summary>
        /// Name of the material.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Number of textures used by this material.
        /// </summary>
        public int TextureCount { get; set; }

        /// <summary>
        /// Number of shader constants.
        /// </summary>
        public int ConstantCount { get; set; }

        /// <summary>
        /// State bits count.
        /// </summary>
        public int StateBitsCount { get; set; }

        /// <summary>
        /// Name of the technique set used by this material.
        /// </summary>
        public string TechniqueSetName { get; set; } = string.Empty;

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
            record.Content = $"Textures: {TextureCount}, TechSet: {TechniqueSetName}";
        }
    }
}
