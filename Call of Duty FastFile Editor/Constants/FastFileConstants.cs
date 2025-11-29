using System.Text;

namespace Call_of_Duty_FastFile_Editor.Constants
{
    /// <summary>
    /// Shared FastFile header constants used across all games.
    /// Game-specific constants are in GameDefinitions folder.
    /// </summary>
    /// <remarks>
    /// FastFile format reference: https://wiki.zeroy.com/index.php?title=Call_of_Duty_5:_FastFile_Format
    /// Header structure: IWffu100 (8 bytes magic) + version (4 bytes) = 12 bytes total
    ///
    /// Magic byte meanings:
    /// - IW = Infinity Ward
    /// - ff = FastFile
    /// - u = compression type (u=unsigned, 0=signed)
    /// - 100 = format version
    /// </remarks>
    public static class FastFileHeaderConstants
    {
        // Magic values
        public const string UnSignedFF = "IWffu100";
        public const string SignedFF = "IWff0100";
        public static readonly byte[] IWffu100Header = Encoding.ASCII.GetBytes(UnSignedFF);

        // Header structure
        public const int HeaderLength = 0xC;           // 12 bytes
        public const int MagicLength = 0x8;            // 8 bytes
        public const int VersionOffset = 0x8;          // Version starts at byte 8
        public const int VersionLength = 0x4;          // 4 bytes

        // Compression block size
        public const int BlockSize = 0x10000;          // 64KB blocks
    }
}
