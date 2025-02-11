namespace Call_of_Duty_FastFile_Editor.Models
{
    public interface IAssetRecordUpdatable
    {
        /// <summary>
        /// Updates the provided ZoneAssetRecord with data from the implementing object.
        /// </summary>
        void UpdateAssetRecord(ref ZoneAssetRecord assetRecord);

        /// <summary>
        /// Position where the file header starts. 
        /// </summary>
        int StartOfFileHeader { get; set; }

        /// <summary>
        /// Position where the file header ends.
        /// </summary>
        int EndOfFileHeader { get; }

        /// <summary>
        /// Position where the file header ends.
        /// </summary>
        int CodeEndPosition { get; }
    }
}
