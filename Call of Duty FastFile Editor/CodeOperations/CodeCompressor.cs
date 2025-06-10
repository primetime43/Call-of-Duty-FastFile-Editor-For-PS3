using System.Text.RegularExpressions;

namespace Call_of_Duty_FastFile_Editor.CodeOperations
{
    public class CodeCompressor
    {
        public static string CompressCode(string text)
        {
            // Remove multiple newlines and replace with double newlines
            text = Regex.Replace(text, "(\\r\\n){2,}", "\r\n\r\n");

            // Remove tabs
            text = Regex.Replace(text, "\\t", "");

            // Remove spaces around parentheses, brackets, and operators
            text = Regex.Replace(text, "\\( ", "(");
            text = Regex.Replace(text, " \\)", ")");
            text = Regex.Replace(text, "\\[ ", "[");
            text = Regex.Replace(text, " \\]", "]");
            text = Regex.Replace(text, ", ", ",");
            text = Regex.Replace(text, " \\+ ", "+");
            text = Regex.Replace(text, " - ", "-");
            text = Regex.Replace(text, " = ", "=");
            text = Regex.Replace(text, " == ", "==");
            text = Regex.Replace(text, " != ", "!=");
            text = Regex.Replace(text, " \\+= ", "+=");
            text = Regex.Replace(text, " -= ", "-=");
            text = Regex.Replace(text, " \\+\\+ ", "++");
            text = Regex.Replace(text, " < ", "<");
            text = Regex.Replace(text, " > ", ">");
            text = Regex.Replace(text, "; ", ";");
            text = Regex.Replace(text, "for ", "for");
            text = Regex.Replace(text, "if  ", "if");

            // Remove whitespace before newlines
            text = Regex.Replace(text, "\\s+\\n", "\n");

            // Remove any remaining tabs
            text = Regex.Replace(text, "\\t", "");

            // Remove all newline characters
            text = Regex.Replace(text, "\\r\\n|\\n|\\r", " ");

            return text;
        }
    }
}