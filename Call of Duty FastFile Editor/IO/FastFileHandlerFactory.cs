using Call_of_Duty_FastFile_Editor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Call_of_Duty_FastFile_Editor.IO
{
    public static class FastFileHandlerFactory
    {
        public static IFastFileHandler GetHandler(FastFile file)
        {
            if (file.IsCod5File) 
                return new CoD5FastFileHandler();
            if (file.IsCod4File) 
                return new CoD4FastFileHandler();
            throw new NotSupportedException("Unknown or unsupported game.");
        }
    }
}
