using Call_of_Duty_FastFile_Editor.Models;
using Call_of_Duty_FastFile_Editor.Services;
using System.Diagnostics;
using System.Text;

namespace Call_of_Duty_FastFile_Editor.ZoneParsers
{
    /// <summary>
    /// Parser for MenuList (menufile) assets.
    ///
    /// MenuList structure in zone:
    /// struct MenuList {
    ///     const char* name;      // 4 bytes - 0xFFFFFFFF when inline
    ///     int menuCount;         // 4 bytes
    ///     menuDef_t** menus;     // 4 bytes - 0xFFFFFFFF when inline
    /// };
    ///
    /// Zone layout when inline:
    /// [FF FF FF FF] [menuCount BE] [FF FF FF FF] [name\0] [menu pointers...] [menu data...]
    /// </summary>
    public static class MenuListParser
    {
        /// <summary>
        /// Parses a MenuList asset starting at the given offset.
        /// </summary>
        public static MenuList ParseMenuList(byte[] zoneData, int offset, bool isBigEndian = true)
        {
            Debug.WriteLine($"[MenuListParser] Parsing MenuList at offset 0x{offset:X}");

            if (offset + 12 > zoneData.Length)
            {
                Debug.WriteLine($"[MenuListParser] Not enough data at offset 0x{offset:X}");
                return null;
            }

            // Check for name pointer marker
            uint namePtr = ReadUInt32(zoneData, offset, isBigEndian);
            if (namePtr != 0xFFFFFFFF)
            {
                Debug.WriteLine($"[MenuListParser] Expected 0xFFFFFFFF at offset 0x{offset:X}, got 0x{namePtr:X}");
                return null;
            }

            // Read menu count
            int menuCount = ReadInt32(zoneData, offset + 4, isBigEndian);
            if (menuCount < 0 || menuCount > 1000)
            {
                Debug.WriteLine($"[MenuListParser] Invalid menu count {menuCount} at offset 0x{offset + 4:X}");
                return null;
            }

            // Check for menus pointer marker
            uint menusPtr = ReadUInt32(zoneData, offset + 8, isBigEndian);
            if (menusPtr != 0xFFFFFFFF)
            {
                Debug.WriteLine($"[MenuListParser] Expected 0xFFFFFFFF at offset 0x{offset + 8:X}, got 0x{menusPtr:X}");
                return null;
            }

            // Read name string (starts after the 12-byte header)
            int nameOffset = offset + 12;
            string name = ReadNullTerminatedString(zoneData, nameOffset);

            if (string.IsNullOrEmpty(name))
            {
                Debug.WriteLine($"[MenuListParser] Empty name at offset 0x{nameOffset:X}");
                return null;
            }

            // Validate name looks like a menu file path
            if (!IsValidMenuFileName(name))
            {
                Debug.WriteLine($"[MenuListParser] Invalid menu file name: '{name}'");
                return null;
            }

            int nameByteCount = Encoding.ASCII.GetByteCount(name) + 1;
            int dataOffset = nameOffset + nameByteCount;

            var menuList = new MenuList
            {
                Name = name,
                MenuCount = menuCount,
                StartOfFileHeader = offset,
                EndOfFileHeader = dataOffset,
                DataStartOffset = dataOffset
            };

            Debug.WriteLine($"[MenuListParser] Found MenuList: '{name}' with {menuCount} menus");

            // Parse individual menus if count > 0
            if (menuCount > 0)
            {
                int currentOffset = dataOffset;

                // First, there are menuCount pointers (each 4 bytes, 0xFFFFFFFF when inline)
                for (int i = 0; i < menuCount && currentOffset + 4 <= zoneData.Length; i++)
                {
                    uint menuPtr = ReadUInt32(zoneData, currentOffset, isBigEndian);
                    currentOffset += 4;

                    // If pointer is 0xFFFFFFFF, menu data follows
                    // If pointer is something else, it's an external reference
                }

                // Now parse each menu definition
                for (int i = 0; i < menuCount && currentOffset < zoneData.Length; i++)
                {
                    var menu = ParseMenuDef(zoneData, currentOffset, isBigEndian);
                    if (menu != null)
                    {
                        menuList.Menus.Add(menu);
                        currentOffset = menu.EndOffset;
                    }
                    else
                    {
                        Debug.WriteLine($"[MenuListParser] Failed to parse menu {i} at offset 0x{currentOffset:X}");
                        break;
                    }
                }

                menuList.DataEndOffset = currentOffset;
            }
            else
            {
                menuList.DataEndOffset = dataOffset;
            }

            Debug.WriteLine($"[MenuListParser] Parsed MenuList '{name}': {menuList.Menus.Count} menus loaded");
            return menuList;
        }

        /// <summary>
        /// Parses a single menuDef_t structure.
        /// This is a complex structure - we'll parse the key fields.
        /// </summary>
        private static MenuDef ParseMenuDef(byte[] zoneData, int offset, bool isBigEndian)
        {
            Debug.WriteLine($"[MenuListParser] Parsing menuDef_t at offset 0x{offset:X}");

            // menuDef_t starts with windowDef_t
            // windowDef_t starts with name pointer
            if (offset + 4 > zoneData.Length)
                return null;

            uint namePtr = ReadUInt32(zoneData, offset, isBigEndian);
            if (namePtr != 0xFFFFFFFF)
            {
                Debug.WriteLine($"[MenuListParser] menuDef_t name pointer not 0xFFFFFFFF at 0x{offset:X}");
                return null;
            }

            // For now, we'll do a simplified parse - just get the window name
            // The full structure is very complex with many nested pointers

            var menu = new MenuDef
            {
                Window = new WindowDef(),
                StartOffset = offset
            };

            // Try to find the window name after the header
            // The exact layout depends on how pointers are resolved
            int searchOffset = offset + 4;
            string windowName = TryFindMenuName(zoneData, searchOffset);

            if (!string.IsNullOrEmpty(windowName))
            {
                menu.Window.Name = windowName;
                Debug.WriteLine($"[MenuListParser] Found menu name: '{windowName}'");
            }

            // Estimate end offset - this is imprecise without full parsing
            // For now, look for the next 0xFFFFFFFF marker that could start another structure
            menu.EndOffset = EstimateMenuEnd(zoneData, offset, isBigEndian);

            return menu;
        }

        /// <summary>
        /// Tries to find the menu name in the structure.
        /// </summary>
        private static string TryFindMenuName(byte[] zoneData, int offset)
        {
            // Look for a valid string after the initial pointer
            for (int i = offset; i < Math.Min(offset + 256, zoneData.Length); i++)
            {
                if (zoneData[i] >= 0x20 && zoneData[i] <= 0x7E)
                {
                    string candidate = ReadNullTerminatedString(zoneData, i);
                    if (!string.IsNullOrEmpty(candidate) && candidate.Length >= 2 && candidate.Length <= 128)
                    {
                        // Check if it looks like a menu name (alphanumeric, underscores, no weird chars)
                        if (IsValidMenuName(candidate))
                        {
                            return candidate;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Estimates where a menu definition ends.
        /// This is approximate - full parsing would require following all pointers.
        /// </summary>
        private static int EstimateMenuEnd(byte[] zoneData, int startOffset, bool isBigEndian)
        {
            // menuDef_t is at least 0x188 bytes on PC
            // On console it may be larger due to array size differences
            int minSize = 0x188;

            if (startOffset + minSize > zoneData.Length)
                return zoneData.Length;

            // For now, return a minimum estimate
            // Full parsing would track all inline data
            return startOffset + minSize;
        }

        /// <summary>
        /// Validates that a string looks like a valid menu file name.
        /// </summary>
        private static bool IsValidMenuFileName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length < 3 || name.Length > 256)
                return false;

            // Should contain path separators or end with .menu
            if (!name.Contains('/') && !name.Contains('\\') && !name.EndsWith(".menu", StringComparison.OrdinalIgnoreCase))
                return false;

            // Check for valid path characters
            foreach (char c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '/' && c != '\\' && c != '.' && c != '-')
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Validates that a string looks like a valid menu name.
        /// </summary>
        private static bool IsValidMenuName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length < 2 || name.Length > 128)
                return false;

            // Check for valid identifier characters
            foreach (char c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Reads a null-terminated ASCII string.
        /// </summary>
        private static string ReadNullTerminatedString(byte[] data, int offset)
        {
            var sb = new StringBuilder();
            while (offset < data.Length && data[offset] != 0)
            {
                char c = (char)data[offset];
                if (c < 0x20 || c > 0x7E)
                    break; // Non-printable character
                sb.Append(c);
                offset++;
            }
            return sb.ToString();
        }

        private static uint ReadUInt32(byte[] data, int offset, bool isBigEndian)
        {
            if (isBigEndian)
            {
                return (uint)((data[offset] << 24) | (data[offset + 1] << 16) |
                              (data[offset + 2] << 8) | data[offset + 3]);
            }
            return BitConverter.ToUInt32(data, offset);
        }

        private static int ReadInt32(byte[] data, int offset, bool isBigEndian)
        {
            return (int)ReadUInt32(data, offset, isBigEndian);
        }
    }
}
