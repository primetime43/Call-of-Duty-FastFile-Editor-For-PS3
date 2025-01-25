using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Call_of_Duty_FastFile_Editor.Models
{
    public class FastFileAssets
    {
        public ZoneAsset_StringTable? StringTable { get; set; }
        public ZoneAsset_RawFileNode? RawFile { get; set; }
        public ZoneAsset_Tags? Tags { get; set; }
    }
}
