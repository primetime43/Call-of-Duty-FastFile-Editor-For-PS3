using System.Text;

namespace Call_of_Duty_FastFile_Editor.Constants
{
    // Both CoD4 & COD5 Header Info
    /// <summary>
    /// The header of an FF file. First 8 bytes of the file are the Magic type and next 4 bytes are the version.
    /// Header is a total of 12 bytes 0xC.
    /// </summary>
    /// 

    // https://wiki.zeroy.com/index.php?title=Call_of_Duty_5:_FastFile_Format
    // IW - Infinity Ward
    // ff - FastFile
    // u - compression
    // 100 - version
    /*
     * Compressions:
        75   fast (pc default)
        30   best
        73   possibly console / xenon (not ZLIB)
     */
    public static class FastFileHeaderConstants
    {
        public const string UnSignedFF = "IWffu100"; // COD5 & CoD4
        public static readonly byte[] IWffu100Header = Encoding.ASCII.GetBytes(UnSignedFF);
        public const string SignedFF = "IWff0100";
        public const int HeaderLength = 0xC;

        // Header Version Offsets
        public const int FFVersionOffset = 0x8;
        public const int VersionValueCoD4 = 0x1;
        public static readonly byte[] CoD4VersionValue = { 0x00, 0x00, 0x00, 0x01 };
        public const int PCVersionValueCoD4 = 0x5;
        public const int VersionValueCoD5 = 0x183;
        public static readonly byte[] WaWVersionValue = { 0x00, 0x00, 0x01, 0x83 };
        public const int PCVersionValueCoD5 = 0x183;
    }
}
