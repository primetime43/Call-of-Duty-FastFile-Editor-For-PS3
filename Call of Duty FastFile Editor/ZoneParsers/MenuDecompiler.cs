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
        /// Shows editable values (strings, colors, rect) with their offsets tracked.
        /// </summary>
        private void DecompileMenuDefInternal(StringBuilder sb, MenuDef menu, int indentLevel)
        {
            string indent = new string('\t', indentLevel);
            string indent2 = new string('\t', indentLevel + 1);

            sb.AppendLine($"{indent}menuDef");
            sb.AppendLine($"{indent}{{");

            // Menu name - get the actual name from binary
            string? actualName = menu.Window?.Name ?? menu.Name;
            bool hasValidName = !string.IsNullOrEmpty(actualName) && actualName != "(unnamed)" &&
                               !actualName.StartsWith("(menu_") && !actualName.StartsWith("(external_");

            if (hasValidName)
            {
                sb.AppendLine($"{indent2}name\t\t\t\"{actualName}\"");

                // Track the string offset for editing
                int nameOffset = FindStringOffset(actualName!, menu.StartOffset, menu.EndOffset);
                if (nameOffset >= 0)
                {
                    _extractedStrings.Add(new MenuString(actualName!, nameOffset, actualName!.Length));
                }
            }
            else
            {
                sb.AppendLine($"{indent2}// name: (not found in binary)");
            }

            sb.AppendLine();

            // Display all editable values from the parsed menu
            if (menu.EditableValues != null && menu.EditableValues.Count > 0)
            {
                sb.AppendLine($"{indent2}// === Editable Binary Values (offset) ===");
                foreach (var value in menu.EditableValues)
                {
                    sb.AppendLine($"{indent2}{value.Name,-16}\t{value.GetDisplayValue()}\t// 0x{value.Offset:X}");
                }
                sb.AppendLine();
            }

            sb.AppendLine($"{indent2}// itemCount: {menu.ItemCount}");
            sb.AppendLine();

            // Event handlers - extract strings from the binary
            sb.AppendLine($"{indent2}// === Event Handler Strings ===");
            ExtractEventHandlers(sb, menu, indent2);

            // Item count
            if (menu.ItemCount > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"{indent2}// === Item Strings ({menu.ItemCount} items) ===");

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
        /// Only shows editable string values.
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
                    sb.AppendLine($"{indent}// String #{itemIndex} (offset 0x{itemString.Offset:X}):");
                    sb.AppendLine($"{indent}\"{itemString.Value}\"");
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
        /// Extracts quoted strings in order and matches them to original strings by index.
        /// </summary>
        public static List<(MenuString original, string newValue)> ParseEditedText(
            string editedText, List<MenuString> originalStrings)
        {
            var changes = new List<(MenuString original, string newValue)>();

            if (originalStrings == null || originalStrings.Count == 0)
                return changes;

            // Extract all quoted strings from the edited text in order
            var editedStrings = ExtractQuotedStrings(editedText);

            // Match by index - edited strings should be in the same order as original
            for (int i = 0; i < originalStrings.Count && i < editedStrings.Count; i++)
            {
                string newValue = editedStrings[i];
                var original = originalStrings[i];

                // Check if the string was modified
                if (newValue != original.Value)
                {
                    changes.Add((original, newValue));
                }
            }

            return changes;
        }

        /// <summary>
        /// Parses edited text and updates MenuValue list with new values.
        /// Returns list of values that were modified.
        /// </summary>
        public static List<MenuValue> ParseEditedValues(string editedText, List<MenuValue> originalValues)
        {
            var modifiedValues = new List<MenuValue>();

            if (originalValues == null || originalValues.Count == 0)
                return modifiedValues;

            // Parse each line looking for value assignments
            var lines = editedText.Split('\n');
            foreach (var line in lines)
            {
                string trimmed = line.Trim();

                // Skip comments and braces
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//") ||
                    trimmed == "{" || trimmed == "}")
                    continue;

                // Look for property name at start of line
                foreach (var value in originalValues)
                {
                    if (trimmed.StartsWith(value.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        // Extract the value part after the property name
                        string valuePart = trimmed.Substring(value.Name.Length).Trim();

                        // Remove trailing comment if present
                        int commentIdx = valuePart.IndexOf("//");
                        if (commentIdx >= 0)
                            valuePart = valuePart.Substring(0, commentIdx).Trim();

                        // Parse the value
                        string originalDisplay = value.GetDisplayValue();
                        if (value.ParseValue(valuePart))
                        {
                            string newDisplay = value.GetDisplayValue();
                            if (newDisplay != originalDisplay)
                            {
                                value.IsModified = true;
                                if (!modifiedValues.Contains(value))
                                    modifiedValues.Add(value);
                            }
                        }
                        break;
                    }
                }
            }

            return modifiedValues;
        }

        /// <summary>
        /// Writes modified MenuValue data back to zone data.
        /// </summary>
        public static void ApplyMenuValueChanges(byte[] zoneData, List<MenuValue> values, bool isBigEndian)
        {
            foreach (var value in values)
            {
                if (!value.IsModified)
                    continue;

                switch (value.Type)
                {
                    case MenuValueType.Color:
                    case MenuValueType.Rect:
                        // Write 4 floats
                        for (int i = 0; i < 4; i++)
                        {
                            WriteFloat(zoneData, value.Offset + i * 4, value.FloatValues[i], isBigEndian);
                        }
                        break;

                    case MenuValueType.Float:
                        WriteFloat(zoneData, value.Offset, value.FloatValues[0], isBigEndian);
                        break;

                    case MenuValueType.Int:
                        WriteInt32(zoneData, value.Offset, value.IntValue, isBigEndian);
                        break;

                    case MenuValueType.String:
                        // String writing handled separately (same as MenuString)
                        break;
                }
            }
        }

        /// <summary>
        /// Writes a float to zone data at the specified offset.
        /// </summary>
        private static void WriteFloat(byte[] data, int offset, float value, bool isBigEndian)
        {
            if (offset + 4 > data.Length)
                return;

            byte[] bytes = BitConverter.GetBytes(value);
            if (isBigEndian)
            {
                // Reverse byte order for big-endian
                data[offset] = bytes[3];
                data[offset + 1] = bytes[2];
                data[offset + 2] = bytes[1];
                data[offset + 3] = bytes[0];
            }
            else
            {
                Array.Copy(bytes, 0, data, offset, 4);
            }
        }

        /// <summary>
        /// Writes an int32 to zone data at the specified offset.
        /// </summary>
        private static void WriteInt32(byte[] data, int offset, int value, bool isBigEndian)
        {
            if (offset + 4 > data.Length)
                return;

            if (isBigEndian)
            {
                data[offset] = (byte)((value >> 24) & 0xFF);
                data[offset + 1] = (byte)((value >> 16) & 0xFF);
                data[offset + 2] = (byte)((value >> 8) & 0xFF);
                data[offset + 3] = (byte)(value & 0xFF);
            }
            else
            {
                byte[] bytes = BitConverter.GetBytes(value);
                Array.Copy(bytes, 0, data, offset, 4);
            }
        }

        /// <summary>
        /// Extracts all quoted string values from the text.
        /// Returns strings in the order they appear.
        /// </summary>
        private static List<string> ExtractQuotedStrings(string text)
        {
            var strings = new List<string>();
            int pos = 0;

            while (pos < text.Length)
            {
                // Find opening quote
                int quoteStart = text.IndexOf('"', pos);
                if (quoteStart < 0)
                    break;

                // Skip if this quote is in a comment line
                int lineStart = text.LastIndexOf('\n', quoteStart);
                if (lineStart < 0) lineStart = 0;
                string linePrefix = text.Substring(lineStart, quoteStart - lineStart);
                if (linePrefix.Contains("//"))
                {
                    pos = quoteStart + 1;
                    continue;
                }

                // Find closing quote
                int quoteEnd = text.IndexOf('"', quoteStart + 1);
                if (quoteEnd < 0)
                    break;

                // Extract the string content (without quotes)
                string value = text.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                strings.Add(value);

                pos = quoteEnd + 1;
            }

            return strings;
        }
    }
}
