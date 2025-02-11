using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Call_of_Duty_FastFile_Editor.Models
{
    public class AssetProcessResult
    {
        public List<RawFileNode> RawFileNodes { get; set; } = new List<RawFileNode>();
        public List<StringTable> StringTables { get; set; } = new List<StringTable>();
        public List<ZoneAssetRecord> UpdatedRecords { get; set; } = new List<ZoneAssetRecord>();
    }

}
