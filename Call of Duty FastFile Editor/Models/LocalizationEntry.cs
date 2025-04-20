using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Call_of_Duty_FastFile_Editor.Models
{
    public class LocalizedEntry : IAssetRecordUpdatable
    {
        public string Key { get; set; }
        public string LocalizedText { get; set; }
        public int StartOfFileHeader { get; set; }
        public int EndOfFileHeader { get; set; }
        public int StartOfFileData { get; set; }
        public int EndOfFileData { get; set; }

        public void UpdateAssetRecord(ref ZoneAssetRecord assetRecord)
        {
            assetRecord.AssetDataStartPosition = this.StartOfFileHeader;
            assetRecord.AssetDataEndOffset = this.EndOfFileHeader;
            assetRecord.Name = this.Key;

            //this is needed for the loop in AssetRecordProcessor
            assetRecord.AssetRecordEndOffset = this.EndOfFileHeader;
        }
    }
}