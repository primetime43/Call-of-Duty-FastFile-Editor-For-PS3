using Ionic.Zlib;
using System.Text;
using Call_of_Duty_FastFile_Editor.Models;
using Call_of_Duty_FastFile_Editor.Constants;

namespace Call_of_Duty_FastFile_Editor.IO
{
    public class CoD5FastFileHandler : FastFileHandlerBase
    {
        protected override byte[] HeaderBytes => FastFileHeaderConstants.IWffu100Header;
        protected override byte[] VersionBytes => FastFileHeaderConstants.CoD5VersionValue;
    }
}
