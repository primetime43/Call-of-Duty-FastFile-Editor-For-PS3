using System;
using System.Text;

namespace Call_of_Duty_FastFile_Editor
{
    public static class Constants
    {
        public static class FastFiles
        {
            // Both CoD4 & WaW
            public const string UnSignedFF = "IWffu100"; // WaW & CoD4
            public static readonly byte[] IWffu100_header = Encoding.ASCII.GetBytes(UnSignedFF); // "IWffu100" header

            public const string SignedFF = "IWff0100";
            public const int FFVersionOffset = 0x8;
            public const int VersionValueCoD4 = 0x1; // XBOX 360 & PS3
            public static readonly byte[] CoD4_VersionValue = new byte[] { 0x00, 0x00, 0x00, 0x01 }; // 0x1 in hex big endian 4 bytes
            public const int PCVersionValueCoD4 = 0x5; // PC
            public const int VersionValueWaW = 0x183; // XBOX 360 & PS3
            public static readonly byte[] WaW_VersionValue = new byte[] { 0x00, 0x00, 0x01, 0x83 }; // 0x183 in hex big endian 4 bytes
            public const int PCVersionValueWaW = 0x183; // PC
        }

        public static class RawFiles
        {
            // Define the byte patterns to search for
            public static readonly byte[][] FileNamePatterns = new byte[][]
            {
                new byte[] { 0x2E, 0x63, 0x66, 0x67, 0x00 },             // .cfg
                new byte[] { 0x2E, 0x67, 0x73, 0x63, 0x00 },             // .gsc
                new byte[] { 0x2E, 0x61, 0x74, 0x72, 0x00 },             // .atr
                new byte[] { 0x2E, 0x63, 0x73, 0x63, 0x00 },             // .csc
                new byte[] { 0x2E, 0x72, 0x6D, 0x62, 0x00 },             // .rmb
                new byte[] { 0x2E, 0x61, 0x72, 0x65, 0x6E, 0x61, 0x00 }, // .arena
                new byte[] { 0x2E, 0x76, 0x69, 0x73, 0x69, 0x6F, 0x6E, 0x00 } // .vision
            };

            // Individual patterns
            public static readonly byte[] Pattern_Cfg = new byte[] { 0x2E, 0x63, 0x66, 0x67, 0x00 }; // .cfg
            public static readonly byte[] Pattern_Gsc = new byte[] { 0x2E, 0x67, 0x73, 0x63, 0x00 }; // .gsc
            public static readonly byte[] Pattern_Atr = new byte[] { 0x2E, 0x61, 0x74, 0x72, 0x00 }; // .atr
            public static readonly byte[] Pattern_Csc = new byte[] { 0x2E, 0x63, 0x73, 0x63, 0x00 }; // .csc
            public static readonly byte[] Pattern_Rmb = new byte[] { 0x2E, 0x72, 0x6D, 0x62, 0x00 }; // .rmb
            public static readonly byte[] Pattern_Arena = new byte[] { 0x2E, 0x61, 0x72, 0x65, 0x6E, 0x61, 0x00 }; // .arena
            public static readonly byte[] Pattern_Vision = new byte[] { 0x2E, 0x76, 0x69, 0x73, 0x69, 0x6F, 0x6E, 0x00 }; // .vision
        }
    }
}