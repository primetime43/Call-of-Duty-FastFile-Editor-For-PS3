namespace Call_of_Duty_FastFile_Editor.Constants
{
    /// <summary>
    /// Zone file header offsets based on XFile and XAssetList structures.
    /// Reference: https://codresearch.dev/index.php/FastFiles_and_Zone_files_(MW2)
    /// </summary>
    public static class ZoneFileHeaderConstants
    {
        // XFile structure
        public const int ZoneSizeOffset = 0x00;              // 4 bytes - Total zone data size
        public const int ExternalSizeOffset = 0x04;          // 4 bytes - External resource allocation size
        public const int BlockSizeTempOffset = 0x08;         // 4 bytes - XFILE_BLOCK_TEMP allocation
        public const int BlockSizePhysicalOffset = 0x0C;     // 4 bytes - XFILE_BLOCK_PHYSICAL allocation
        public const int BlockSizeRuntimeOffset = 0x10;      // 4 bytes - XFILE_BLOCK_RUNTIME allocation
        public const int BlockSizeVirtualOffset = 0x14;      // 4 bytes - XFILE_BLOCK_VIRTUAL allocation
        public const int BlockSizeLargeOffset = 0x18;        // 4 bytes - XFILE_BLOCK_LARGE allocation
        public const int BlockSizeCallbackOffset = 0x1C;     // 4 bytes - XFILE_BLOCK_CALLBACK allocation
        public const int BlockSizeVertexOffset = 0x20;       // 4 bytes - XFILE_BLOCK_VERTEX allocation (PS3/PC)

        // XAssetList structure
        public const int ScriptStringCountOffset = 0x24;     // 4 bytes - Number of script strings (tags)
        public const int ScriptStringsPtrOffset = 0x28;      // 4 bytes - Pointer to script strings (0xFFFFFFFF placeholder)
        public const int AssetCountOffset = 0x2C;            // 4 bytes - Number of assets in zone
        public const int AssetsPtrOffset = 0x30;             // 4 bytes - Pointer to assets array (0xFFFFFFFF placeholder)
    }
}
