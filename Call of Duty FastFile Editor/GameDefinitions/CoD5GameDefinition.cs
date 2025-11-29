using Call_of_Duty_FastFile_Editor.Models;

namespace Call_of_Duty_FastFile_Editor.GameDefinitions
{
    /// <summary>
    /// Game definition implementation for Call of Duty: World at War (CoD5).
    /// Uses the default CoD4/CoD5 rawfile parsing structure.
    /// </summary>
    public class CoD5GameDefinition : GameDefinitionBase
    {
        public override string GameName => CoD5Definition.GameName;
        public override string ShortName => "COD5";
        public override int VersionValue => CoD5Definition.VersionValue;
        public override int PCVersionValue => CoD5Definition.PCVersionValue;
        public override byte[] VersionBytes => CoD5Definition.VersionBytes;
        public override byte RawFileAssetType => CoD5Definition.RawFileAssetType;
        public override byte LocalizeAssetType => CoD5Definition.LocalizeAssetType;

        public override string GetAssetTypeName(int assetType)
        {
            if (Enum.IsDefined(typeof(CoD5AssetType), assetType))
            {
                return ((CoD5AssetType)assetType).ToString();
            }
            return $"unknown_0x{assetType:X2}";
        }

        public override bool IsSupportedAssetType(int assetType)
        {
            return assetType == RawFileAssetType || assetType == LocalizeAssetType;
        }
    }
}
