using Call_of_Duty_FastFile_Editor.Models;
using System.Text;

namespace Call_of_Duty_FastFile_Editor.ZoneParsers
{
    /// <summary>
    /// Decompiles binary menu data into readable text format similar to source .menu files.
    /// Tracks string offsets for editing and saving back to zone.
    /// </summary>
    public class MenuDecompiler
    {
        private readonly byte[] _zoneData;
        private readonly bool _isBigEndian;
        private readonly List<MenuString> _extractedStrings;

        public MenuDecompiler(byte[] zoneData, bool isBigEndian)
        {
            _zoneData = zoneData;
            _isBigEndian = isBigEndian;
            _extractedStrings = new List<MenuString>();
        }

        /// <summary>
        /// Decompiles a MenuList into formatted text and extracts editable strings with offsets.
        /// </summary>
        public (string formattedText, List<MenuString> strings) DecompileMenuList(MenuList menuList)
        {
            _extractedStrings.Clear();
            var sb = new StringBuilder();

            // Menu file header
            sb.AppendLine("{");

            // Decompile each menu in the list
            foreach (var menu in menuList.Menus)
            {
                DecompileMenuDefInternal(sb, menu, 1);
            }

            sb.AppendLine("}");

            return (sb.ToString(), new List<MenuString>(_extractedStrings));
        }

        /// <summary>
        /// Decompiles a single MenuDef into formatted text and extracts editable strings with offsets.
        /// </summary>
        public (string formattedText, List<MenuString> strings) DecompileMenuDef(MenuDef menu)
        {
            _extractedStrings.Clear();
            var sb = new StringBuilder();

            sb.AppendLine("{");
            DecompileMenuDefInternal(sb, menu, 1);
            sb.AppendLine("}");

            return (sb.ToString(), new List<MenuString>(_extractedStrings));
        }

        /// <summary>
        /// Internal method to decompile a single menuDef_t structure.
        /// </summary>
        private void DecompileMenuDefInternal(StringBuilder sb, MenuDef menu, int indentLevel)
        {
            string indent = new string('\t', indentLevel);
            string indent2 = new string('\t', indentLevel + 1);

            sb.AppendLine($"{indent}menuDef");
            sb.AppendLine($"{indent}{{");

            // Menu name - get the actual name from binary or show placeholder (not editable)
            string? actualName = menu.Window?.Name ?? menu.Name;
            bool hasValidName = !string.IsNullOrEmpty(actualName);

            if (hasValidName)
            {
                sb.AppendLine($"{indent2}name\t\t\t{actualName}");

                // Only track strings that actually exist in the binary data
                // Placeholder values like "(unnamed)" should NOT be saved back
                int nameOffset = FindStringOffset(actualName!, menu.StartOffset, menu.EndOffset);
                if (nameOffset >= 0)
                {
                    _extractedStrings.Add(new MenuString(actualName!, nameOffset, actualName!.Length));
                }
            }
            else
            {
                // Show placeholder - this is NOT editable and will NOT be saved
                sb.AppendLine($"{indent2}name\t\t\t// (no name found in binary)");
            }

            // Fullscreen, visible
            sb.AppendLine($"{indent2}fullscreen\t\t0");
            sb.AppendLine($"{indent2}visible\t\t\t0");

            // Rect
            if (menu.Window?.Rect != null)
            {
                var r = menu.Window.Rect;
                sb.AppendLine($"{indent2}rect\t\t\t{r.X} {r.Y} {r.W} {r.H} {r.HorzAlign} {r.VertAlign}");
            }
            else
            {
                sb.AppendLine($"{indent2}rect\t\t\t0 0 640 480 0 0");
            }

            sb.AppendLine($"{indent2}style\t\t\t0");
            sb.AppendLine($"{indent2}border\t\t\t0");
            sb.AppendLine($"{indent2}ownerDraw\t\t0");
            sb.AppendLine($"{indent2}ownerDrawFlag\t\t0");
            sb.AppendLine($"{indent2}borderSize\t\t0");

            // Colors
            sb.AppendLine($"{indent2}foreColor\t\t1 1 1 1");
            sb.AppendLine($"{indent2}backColor\t\t0 0 0 0");
            sb.AppendLine($"{indent2}borderColor\t\t0 0 0 0");
            sb.AppendLine($"{indent2}outlineColor\t\t0 0 0 0");
            sb.AppendLine($"{indent2}disableColor\t\t0 0 0 0");

            // Fade settings
            sb.AppendLine($"{indent2}fadeCycle\t\t0");
            sb.AppendLine($"{indent2}fadeClamp\t\t0");
            sb.AppendLine($"{indent2}fadeAmount\t\t0");
            sb.AppendLine($"{indent2}fadeInAmount\t\t0");
            sb.AppendLine($"{indent2}blurWorld\t\t0");

            // Event handlers - extract strings from the binary
            ExtractEventHandlers(sb, menu, indent2);

            // Item count
            if (menu.ItemCount > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"{indent2}// {menu.ItemCount} items");

                // Extract item data
                ExtractItemDefs(sb, menu, indentLevel + 1);
            }

            sb.AppendLine($"{indent}}}");
            sb.AppendLine();
        }

        /// <summary>
        /// Extracts event handler strings from the menu data.
        /// </summary>
        private void ExtractEventHandlers(StringBuilder sb, MenuDef menu, string indent)
        {
            // Search for common event handler patterns in the binary data
            var handlers = new[] { "onOpen", "onClose", "onEsc", "onFocus", "onFocusLost" };

            foreach (var handler in handlers)
            {
                sb.AppendLine($"{indent}{handler}");
                sb.AppendLine($"{indent}{{");

                // Try to find script expressions for this handler
                var expressions = FindScriptExpressions(menu.StartOffset, menu.EndOffset, handler);
                foreach (var expr in expressions)
                {
                    sb.AppendLine($"{indent}\t{expr.Value}");
                    _extractedStrings.Add(expr);
                }

                sb.AppendLine($"{indent}}}");
            }
        }

        /// <summary>
        /// Extracts itemDef structures from the menu.
        /// </summary>
        private void ExtractItemDefs(StringBuilder sb, MenuDef menu, int indentLevel)
        {
            string indent = new string('\t', indentLevel);
            string indent2 = new string('\t', indentLevel + 1);

            // Extract all strings that look like item-related data
            var itemStrings = ExtractItemStrings(menu.StartOffset, menu.EndOffset);

            int itemIndex = 0;
            foreach (var itemString in itemStrings)
            {
                if (IsItemRelatedString(itemString.Value))
                {
                    sb.AppendLine($"{indent}itemDef");
                    sb.AppendLine($"{indent}{{");
                    sb.AppendLine($"{indent2}text\t\t\t\"{itemString.Value}\"");
                    sb.AppendLine($"{indent2}visible\t\t\t0");
                    sb.AppendLine($"{indent2}rect\t\t\t0 0 0 0 0 0");
                    sb.AppendLine($"{indent2}style\t\t\t0");
                    sb.AppendLine($"{indent2}border\t\t\t0");
                    sb.AppendLine($"{indent2}foreColor\t\t1 1 1 1");
                    sb.AppendLine($"{indent2}backColor\t\t0 0 0 0");
                    sb.AppendLine($"{indent}}}");
                    sb.AppendLine();

                    _extractedStrings.Add(itemString);
                    itemIndex++;
                }
            }
        }

        /// <summary>
        /// Finds the offset of a string in the zone data within the specified range.
        /// </summary>
        private int FindStringOffset(string searchString, int startOffset, int endOffset)
        {
            if (string.IsNullOrEmpty(searchString))
                return -1;

            byte[] searchBytes = Encoding.ASCII.GetBytes(searchString);

            for (int i = startOffset; i < endOffset - searchBytes.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < searchBytes.Length; j++)
                {
                    if (_zoneData[i + j] != searchBytes[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found && (i + searchBytes.Length >= _zoneData.Length || _zoneData[i + searchBytes.Length] == 0))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Finds script expressions in the binary data.
        /// </summary>
        private List<MenuString> FindScriptExpressions(int startOffset, int endOffset, string context)
        {
            var expressions = new List<MenuString>();

            // Search for common expression patterns
            string[] patterns = { "setdvar", "exec", "play", "open", "close", "toggle" };

            int pos = startOffset;
            while (pos < endOffset - 4)
            {
                // Look for printable strings
                if (_zoneData[pos] >= 0x20 && _zoneData[pos] <= 0x7E)
                {
                    string str = ReadNullTerminatedString(pos);
                    if (str.Length >= 3 && ContainsPattern(str, patterns))
                    {
                        expressions.Add(new MenuString(str, pos, str.Length));
                        pos += str.Length + 1;
                        continue;
                    }
                }
                pos++;
            }

            return expressions;
        }

        /// <summary>
        /// Extracts strings that might be item-related.
        /// </summary>
        private List<MenuString> ExtractItemStrings(int startOffset, int endOffset)
        {
            var strings = new List<MenuString>();
            int pos = startOffset;
            var currentString = new StringBuilder();
            int stringStart = pos;

            while (pos < endOffset)
            {
                byte b = _zoneData[pos];

                if (b >= 0x20 && b <= 0x7E)
                {
                    if (currentString.Length == 0)
                        stringStart = pos;
                    currentString.Append((char)b);
                }
                else if (b == 0x00 && currentString.Length >= 2)
                {
                    string str = currentString.ToString();
                    if (IsMenuString(str))
                    {
                        strings.Add(new MenuString(str, stringStart, str.Length));
                    }
                    currentString.Clear();
                }
                else
                {
                    currentString.Clear();
                }
                pos++;
            }

            return strings;
        }

        private bool IsItemRelatedString(string str)
        {
            // Filter for strings that look like UI item text
            if (str.StartsWith("@") || str.Contains("_") ||
                str.Contains("setdvar") || str.Contains("exec"))
                return true;
            return false;
        }

        private bool IsMenuString(string str)
        {
            if (str.Length < 2 || str.Length > 500)
                return false;

            int alphaCount = str.Count(c => char.IsLetterOrDigit(c));
            return alphaCount >= str.Length / 2;
        }

        private bool ContainsPattern(string str, string[] patterns)
        {
            string lower = str.ToLower();
            foreach (var pattern in patterns)
            {
                if (lower.Contains(pattern))
                    return true;
            }
            return false;
        }

        private string ReadNullTerminatedString(int offset)
        {
            var sb = new StringBuilder();
            while (offset < _zoneData.Length && _zoneData[offset] != 0)
            {
                char c = (char)_zoneData[offset];
                if (c < 0x20 || c > 0x7E)
                    break;
                sb.Append(c);
                offset++;
                if (sb.Length > 500) break;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Parses edited text back into strings for saving.
        /// Matches edited strings to their original offsets.
        /// </summary>
        public static List<(MenuString original, string newValue)> ParseEditedText(
            string editedText, List<MenuString> originalStrings)
        {
            var changes = new List<(MenuString original, string newValue)>();

            // Extract all quoted and unquoted strings from the edited text
            var editedStrings = ExtractStringsFromText(editedText);

            // Match edited strings with original strings
            foreach (var original in originalStrings)
            {
                // Try to find this string in the edited text
                string matchedNew = FindMatchingString(original.Value, editedStrings);
                if (matchedNew != null && matchedNew != original.Value)
                {
                    changes.Add((original, matchedNew));
                }
            }

            return changes;
        }

        /// <summary>
        /// Extracts all string values from formatted menu text.
        /// </summary>
        private static List<string> ExtractStringsFromText(string text)
        {
            var strings = new List<string>();
            var lines = text.Split('\n');

            foreach (var line in lines)
            {
                string trimmed = line.Trim();

                // Skip comments and braces
                if (trimmed.StartsWith("//") || trimmed == "{" || trimmed == "}")
                    continue;

                // Extract property values
                // Pattern: property_name<whitespace>value
                int tabIndex = trimmed.IndexOfAny(new[] { '\t', ' ' });
                if (tabIndex > 0)
                {
                    string value = trimmed.Substring(tabIndex).Trim();

                    // Handle quoted strings
                    if (value.StartsWith("\"") && value.EndsWith("\""))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    if (!string.IsNullOrEmpty(value) && !IsNumericValue(value))
                    {
                        strings.Add(value);
                    }
                }
            }

            return strings;
        }

        private static string? FindMatchingString(string original, List<string> editedStrings)
        {
            // First try exact match
            foreach (var edited in editedStrings)
            {
                if (edited == original)
                    return edited;
            }

            // Try to find by similarity (for edited strings)
            foreach (var edited in editedStrings)
            {
                // Check if the edited string is a modified version of the original
                if (IsSimilar(original, edited))
                    return edited;
            }

            return null;
        }

        private static bool IsSimilar(string a, string b)
        {
            // Simple similarity check - strings that share significant prefix
            if (a.Length == 0 || b.Length == 0)
                return false;

            int commonPrefix = 0;
            int minLen = Math.Min(a.Length, b.Length);
            for (int i = 0; i < minLen; i++)
            {
                if (a[i] == b[i])
                    commonPrefix++;
                else
                    break;
            }

            return commonPrefix >= minLen / 2;
        }

        private static bool IsNumericValue(string value)
        {
            // Check if value is numeric (including floats and color values)
            return value.All(c => char.IsDigit(c) || c == '.' || c == '-' || c == ' ');
        }
    }
}
