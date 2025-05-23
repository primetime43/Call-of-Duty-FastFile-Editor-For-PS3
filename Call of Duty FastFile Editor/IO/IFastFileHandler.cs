using Call_of_Duty_FastFile_Editor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Call_of_Duty_FastFile_Editor.IO
{
    public interface IFastFileHandler
    {
        void Decompress(string inputFilePath, string outputFilePath);
        void Recompress(string ffFilePath, string zoneFilePath, FastFile openedFastFile);
        // string GameName { get; } change this later. add more fields
    }
}
