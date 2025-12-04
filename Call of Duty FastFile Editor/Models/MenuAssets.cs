using System.Text;

namespace Call_of_Duty_FastFile_Editor.Models
{
    /// <summary>
    /// Rectangle definition used in menu windows.
    /// Size: 0x14 bytes (with padding)
    /// </summary>
    public class RectDef
    {
        public float X { get; set; }        // 0x00
        public float Y { get; set; }        // 0x04
        public float W { get; set; }        // 0x08
        public float H { get; set; }        // 0x0C
        public byte HorzAlign { get; set; } // 0x10
        public byte VertAlign { get; set; } // 0x11
        // 2 bytes padding to 0x14

        public const int Size = 0x14;

        public override string ToString() => $"({X}, {Y}, {W}, {H})";
    }

    /// <summary>
    /// Window definition - base structure for menus and items.
    /// Size: 0xA4 bytes (PC), varies on console
    /// </summary>
    public class WindowDef
    {
        public string Name { get; set; }            // 0x00 - pointer
        public RectDef Rect { get; set; }           // 0x04
        public RectDef RectClient { get; set; }     // 0x18
        public string Group { get; set; }           // 0x2C - pointer
        public int Style { get; set; }              // 0x30
        public int Border { get; set; }             // 0x34
        public int OwnerDraw { get; set; }          // 0x38
        public int OwnerDrawFlags { get; set; }     // 0x3C
        public float BorderSize { get; set; }       // 0x40
        public int StaticFlags { get; set; }        // 0x44
        public int[] DynamicFlags { get; set; }     // 0x48 - size varies
        public int NextTime { get; set; }           // 0x4C (PC) or later on console
        public float[] ForeColor { get; set; }      // vec4_t
        public float[] BackColor { get; set; }      // vec4_t
        public float[] BorderColor { get; set; }    // vec4_t
        public float[] OutlineColor { get; set; }   // vec4_t
        public float[] DisableColor { get; set; }   // vec4_t
        public string BackgroundMaterial { get; set; } // pointer to Material

        // Size varies between PC (0xA4) and console versions
        public const int PCSize = 0xA4;
        public const int ConsoleSize = 0xB0; // Approximate - has larger arrays
    }

    /// <summary>
    /// Menu item definition.
    /// </summary>
    public class ItemDef
    {
        public WindowDef Window { get; set; }
        public RectDef[] TextRect { get; set; }     // 1 on PC, 4 on console
        public int Type { get; set; }
        public int DataType { get; set; }
        public int Alignment { get; set; }
        public int FontEnum { get; set; }
        public int TextAlignMode { get; set; }
        public float TextAlignX { get; set; }
        public float TextAlignY { get; set; }
        public float TextScale { get; set; }
        public int TextStyle { get; set; }
        public string Text { get; set; }
        public string Dvar { get; set; }
        public string DvarTest { get; set; }
        public string EnableDvar { get; set; }
        public int DvarFlags { get; set; }
        public float Special { get; set; }
        public int[] CursorPos { get; set; }

        // Many more fields omitted for initial implementation
    }

    /// <summary>
    /// Menu definition - the main menu structure.
    /// </summary>
    public class MenuDef
    {
        public WindowDef Window { get; set; }
        public string Font { get; set; }            // 0xA4 - pointer
        public int Fullscreen { get; set; }         // 0xA8
        public int ItemCount { get; set; }          // 0xAC
        public int FontIndex { get; set; }          // 0xB0
        public int[] CursorItems { get; set; }      // 0xB4
        public int FadeCycle { get; set; }
        public float FadeClamp { get; set; }
        public float FadeAmount { get; set; }
        public float FadeInAmount { get; set; }
        public float BlurRadius { get; set; }
        public string AllowedBinding { get; set; }
        public string SoundName { get; set; }
        public int ImageTrack { get; set; }
        public float[] FocusColor { get; set; }     // vec4_t
        public List<ItemDef> Items { get; set; }

        // Zone parsing info
        public int StartOffset { get; set; }
        public int EndOffset { get; set; }

        public MenuDef()
        {
            Items = new List<ItemDef>();
            FocusColor = new float[4];
            CursorItems = new int[1]; // PC default
        }

        public string Name => Window?.Name ?? "(unnamed)";
    }

    /// <summary>
    /// MenuList/MenuFile asset - contains a list of menus.
    /// Zone structure: [name_ptr] [menuCount] [menus_ptr] [name\0] [menu data...]
    /// </summary>
    public class MenuList : IAssetRecordUpdatable
    {
        public string Name { get; set; }
        public int MenuCount { get; set; }
        public List<MenuDef> Menus { get; set; }

        // Zone parsing info
        public int StartOfFileHeader { get; set; }
        public int EndOfFileHeader { get; set; }
        public int DataStartOffset { get; set; }
        public int DataEndOffset { get; set; }

        /// <summary>
        /// Static property to hold the currently loaded zone.
        /// </summary>
        public static ZoneFile CurrentZone { get; set; }

        public MenuList()
        {
            Menus = new List<MenuDef>();
        }

        public void UpdateAssetRecord(ref ZoneAssetRecord assetRecord)
        {
            assetRecord.HeaderStartOffset = StartOfFileHeader;
            assetRecord.HeaderEndOffset = EndOfFileHeader;
            assetRecord.AssetDataStartPosition = DataStartOffset;
            assetRecord.AssetDataEndOffset = DataEndOffset;
            assetRecord.AssetRecordEndOffset = DataEndOffset;
            assetRecord.Name = Name;
            assetRecord.Size = MenuCount;
            assetRecord.Content = $"MenuList with {MenuCount} menus";
            assetRecord.AdditionalData = string.Join(", ", Menus.Select(m => m.Name));
        }

        public override string ToString() => $"MenuList: {Name} ({MenuCount} menus)";
    }

    /// <summary>
    /// Item types for menu items
    /// </summary>
    public enum ItemType
    {
        Text = 0,
        Button = 1,
        RadioButton = 2,
        CheckBox = 3,
        EditField = 4,
        Combo = 5,
        ListBox = 6,
        Model = 7,
        OwnerDraw = 8,
        NumericField = 9,
        Slider = 10,
        YesNo = 11,
        Multi = 12,
        Enum = 13,
        Bind = 14,
        MenuModel = 15,
        ValidFileField = 16,
        DecimalField = 17,
        UpCredit = 18,
        News = 19,
        TextScroll = 20,
        EmailField = 21,
        PasswordField = 22
    }
}
