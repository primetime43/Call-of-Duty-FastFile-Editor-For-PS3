using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Call_of_Duty_FastFile_Editor.Models
{
    public class ZoneFileAssets
    {
        public List<StringTable> StringTables { get; set; } = new List<StringTable>();
        public List<RawFileNode> RawFiles { get; set; } = new List<RawFileNode>();
        public Tags? Tags { get; set; } = new Tags();
    }
}
