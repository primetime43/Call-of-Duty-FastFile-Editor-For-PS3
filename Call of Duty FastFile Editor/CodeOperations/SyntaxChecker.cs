namespace Call_of_Duty_FastFile_Editor.CodeOperations
{
    public class SyntaxChecker
    {
        public static void CheckSyntax(string codeText)
        {
            int openBraces = 0; // Open braces {
            int closeBraces = 0; // Close braces }
            int openParentheses = 0; // Open parentheses (
            int closeParentheses = 0; // Close parentheses )
            int doubleQuotes = 0; // Double quotes "
            int singleQuotes = 0; // Single quotes '
            int openBrackets = 0; // Open brackets [
            int closeBrackets = 0; // Close brackets ]
            bool insideString = false;
            bool insideChar = false;
            string[] lines = codeText.Split('\n');
            List<string> includes = new List<string>();
            List<string> functionDeclarations = new List<string>();

            int lineNumber = 0;
            foreach (string line in lines)
            {
                lineNumber++;
                string trimmedLine = line.Trim();
                int length = trimmedLine.Length;
                if (trimmedLine.Contains("//"))
                {
                    length = trimmedLine.IndexOf("//");
                }
                string codeLine = trimmedLine.Substring(0, length);

                for (int i = 0; i < codeLine.Length; i++)
                {
                    char c = codeLine[i];

                    if (c == '\"' && !insideChar)
                    {
                        insideString = !insideString;
                        doubleQuotes++;
                    }
                    else if (c == '\'' && !insideString)
                    {
                        insideChar = !insideChar;
                        singleQuotes++;
                    }
                    else if (!insideString && !insideChar)
                    {
                        switch (c)
                        {
                            case '{':
                                openBraces++;
                                break;
                            case '}':
                                closeBraces++;
                                break;
                            case '(':
                                openParentheses++;
                                break;
                            case ')':
                                closeParentheses++;
                                break;
                            case '[':
                                openBrackets++;
                                break;
                            case ']':
                                closeBrackets++;
                                break;
                        }
                    }

                    // Check for escape sequences
                    if ((c == '\\' && (insideString || insideChar)) && (i + 1 < codeLine.Length))
                    {
                        i++; // Skip the next character because it's part of an escape sequence
                    }
                }

                if (codeLine.Contains("#include"))
                {
                    includes.Add(codeLine.Substring(codeLine.IndexOf("#include") + 8).Trim());
                }

                if (openBraces == closeBraces && codeLine.Contains("}") && codeLine.Contains("#include") && codeLine.Contains("("))
                {
                    functionDeclarations.Add(codeLine.Substring(0, codeLine.IndexOf("(")).Trim());
                }

                // Check for unmatched symbols and report the first occurrence
                if (openBraces < closeBraces)
                {
                    ShowSyntaxError("Unmatched close brace '}'", lineNumber);
                    return;
                }
                if (openParentheses < closeParentheses)
                {
                    ShowSyntaxError("Unmatched close parenthesis ')'", lineNumber);
                    return;
                }
                if (openBrackets < closeBrackets)
                {
                    ShowSyntaxError("Unmatched close bracket ']'", lineNumber);
                    return;
                }
                if (!IsBalanced(doubleQuotes))
                {
                    ShowSyntaxError("Unmatched double quote '\"'", lineNumber);
                    return;
                }
                if (!IsBalanced(singleQuotes))
                {
                    ShowSyntaxError("Unmatched single quote '\''", lineNumber);
                    return;
                }
            }

            if (openBraces != closeBraces)
            {
                MessageBox.Show("Syntax error: Unmatched braces", "Finished", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
            else if (openParentheses != closeParentheses)
            {
                MessageBox.Show("Syntax error: Unmatched parentheses", "Finished", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
            else if (openBrackets != closeBrackets)
            {
                MessageBox.Show("Syntax error: Unmatched brackets", "Finished", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
            else if (!IsBalanced(doubleQuotes))
            {
                MessageBox.Show("Syntax error: Unmatched double quotes", "Finished", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
            else if (!IsBalanced(singleQuotes))
            {
                MessageBox.Show("Syntax error: Unmatched single quotes", "Finished", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
            else
            {
                MessageBox.Show("Finished checking syntax.\nNo Errors Detected.\n\nSyntax Check provided by EliteMossy", "Finished", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }

        private static bool IsBalanced(int count)
        {
            return count % 2 == 0;
        }

        private static void ShowSyntaxError(string message, int lineNumber)
        {
            MessageBox.Show($"Syntax error: {message} at line {lineNumber}", "Syntax Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}