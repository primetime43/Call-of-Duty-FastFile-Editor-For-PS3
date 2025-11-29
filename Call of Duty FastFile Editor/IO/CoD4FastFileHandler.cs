using Call_of_Duty_FastFile_Editor.Constants;
using Call_of_Duty_FastFile_Editor.GameDefinitions;

namespace Call_of_Duty_FastFile_Editor.IO
{
    public class CoD4FastFileHandler : FastFileHandlerBase
    {
        protected override byte[] HeaderBytes => FastFileHeaderConstants.IWffu100Header;
        protected override byte[] VersionBytes => CoD4Definition.VersionBytes;
    }
}
