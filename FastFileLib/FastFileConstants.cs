namespace FastFileLib;

/// <summary>
/// Constants for FastFile compilation and processing.
/// </summary>
public static class FastFileConstants
{
    public const string UnsignedHeader = "IWffu100";
    public const string SignedHeader = "IWff0100";
    public const int HeaderSize = 12; // 8 bytes magic + 4 bytes version
    public const int BlockSize = 65536; // 0x10000 - 64KB blocks

    // Version bytes (big-endian)
    public static readonly byte[] CoD4Version = { 0x00, 0x00, 0x00, 0x01 };
    public static readonly byte[] WaWVersion = { 0x00, 0x00, 0x01, 0x83 };
    public static readonly byte[] MW2Version = { 0x00, 0x00, 0x01, 0x0D };

    // Asset type IDs for rawfile
    public const byte CoD4RawFileAssetType = 0x21; // 33
    public const byte WaWRawFileAssetType = 0x22;  // 34
    public const byte MW2RawFileAssetType = 0x23;  // 35

    // Asset type IDs for localize
    public const byte CoD4LocalizeAssetType = 0x18; // 24
    public const byte WaWLocalizeAssetType = 0x19;  // 25
    public const byte MW2LocalizeAssetType = 0x1A;  // 26

    // Zone header memory allocation values (big-endian)
    public static readonly byte[] CoD4MemAlloc1 = { 0x00, 0x00, 0x0F, 0x70 };
    public static readonly byte[] WaWMemAlloc1 = { 0x00, 0x00, 0x10, 0xB0 };
    public static readonly byte[] MW2MemAlloc1 = { 0x00, 0x00, 0x03, 0xB4 };

    public static readonly byte[] CoD4MemAlloc2 = { 0x00, 0x00, 0x00, 0x00 };
    public static readonly byte[] WaWMemAlloc2 = { 0x00, 0x05, 0xF8, 0xF0 };
    public static readonly byte[] MW2MemAlloc2 = { 0x00, 0x00, 0x10, 0x00 };

    public static byte[] GetVersionBytes(GameVersion version) => version switch
    {
        GameVersion.CoD4 => CoD4Version,
        GameVersion.WaW => WaWVersion,
        GameVersion.MW2 => MW2Version,
        _ => throw new ArgumentOutOfRangeException(nameof(version))
    };

    public static byte GetRawFileAssetType(GameVersion version) => version switch
    {
        GameVersion.CoD4 => CoD4RawFileAssetType,
        GameVersion.WaW => WaWRawFileAssetType,
        GameVersion.MW2 => MW2RawFileAssetType,
        _ => throw new ArgumentOutOfRangeException(nameof(version))
    };

    public static byte GetLocalizeAssetType(GameVersion version) => version switch
    {
        GameVersion.CoD4 => CoD4LocalizeAssetType,
        GameVersion.WaW => WaWLocalizeAssetType,
        GameVersion.MW2 => MW2LocalizeAssetType,
        _ => throw new ArgumentOutOfRangeException(nameof(version))
    };

    public static byte[] GetMemAlloc1(GameVersion version) => version switch
    {
        GameVersion.CoD4 => CoD4MemAlloc1,
        GameVersion.WaW => WaWMemAlloc1,
        GameVersion.MW2 => MW2MemAlloc1,
        _ => throw new ArgumentOutOfRangeException(nameof(version))
    };

    public static byte[] GetMemAlloc2(GameVersion version) => version switch
    {
        GameVersion.CoD4 => CoD4MemAlloc2,
        GameVersion.WaW => WaWMemAlloc2,
        GameVersion.MW2 => MW2MemAlloc2,
        _ => throw new ArgumentOutOfRangeException(nameof(version))
    };
}
