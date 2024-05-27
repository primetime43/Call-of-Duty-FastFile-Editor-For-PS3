using System;
using System.IO;
using System.Text;

namespace Call_of_Duty_FastFile_Editor.CodeOperations
{
    public class CommentRemover
    {
        // Enum to represent the current state of comment processing
        private enum CommentState
        {
            None,
            SingleLine,
            MultiLine
        }

        // Method to remove C-style comments (/* ... */)
        public static string RemoveCStyleComments(string input)
        {
            StringReader reader = new StringReader(input);
            StringBuilder result = new StringBuilder();
            CommentState state = CommentState.None;
            string line;
            int lineNumber = 0;

            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;
                int multiLineCommentStart = line.IndexOf("/*");
                int singleLineCommentStart = line.IndexOf("//");

                if (state != CommentState.MultiLine)
                {
                    if (multiLineCommentStart != -1)
                    {
                        state = CommentState.MultiLine;
                        if (singleLineCommentStart != -1 && singleLineCommentStart < multiLineCommentStart)
                        {
                            state = CommentState.SingleLine;
                        }
                        if (state == CommentState.MultiLine)
                        {
                            if (!IsCompleteComment(line, multiLineCommentStart))
                            {
                                if (line.Length > 1)
                                {
                                    result.Append(line.Substring(0, multiLineCommentStart));
                                }
                            }
                            else
                            {
                                result.Append(line + "\n");
                                state = CommentState.None;
                            }
                        }
                    }
                    else if (singleLineCommentStart != -1)
                    {
                        state = CommentState.SingleLine;
                    }
                    else
                    {
                        result.Append(line + Environment.NewLine);
                    }
                }
                if (state == CommentState.SingleLine)
                {
                    if (!IsCompleteComment(line, singleLineCommentStart))
                    {
                        if (line.Length > 1)
                        {
                            result.Append(line.Substring(0, singleLineCommentStart));
                        }
                        state = CommentState.None;
                    }
                    else
                    {
                        result.Append(line + Environment.NewLine);
                    }
                }
                if (state != CommentState.MultiLine)
                {
                    continue;
                }
                int multiLineCommentEnd = line.IndexOf("*/");
                if (multiLineCommentEnd != -1)
                {
                    multiLineCommentEnd += 2;
                    if (multiLineCommentEnd > line.Length)
                    {
                        result.Append(line.Substring(multiLineCommentEnd, line.Length - 1));
                    }
                    state = CommentState.None;
                }
            }
            reader.Close();
            return result.ToString();
        }

        // Method to remove custom comments (/# ... #/)
        public static string RemoveCustomComments(string input)
        {
            StringReader reader = new StringReader(input);
            StringBuilder result = new StringBuilder();
            CommentState state = CommentState.None;
            string line;
            int lineNumber = 0;

            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;
                int customCommentStart = line.IndexOf("/#");
                int singleLineCommentStart = line.IndexOf("//");

                if (state != CommentState.MultiLine)
                {
                    if (customCommentStart != -1)
                    {
                        state = CommentState.MultiLine;
                        if (singleLineCommentStart != -1 && singleLineCommentStart < customCommentStart)
                        {
                            state = CommentState.SingleLine;
                        }
                        if (state == CommentState.MultiLine)
                        {
                            if (!IsCompleteComment(line, customCommentStart))
                            {
                                if (line.Length > 1)
                                {
                                    result.Append(line.Substring(0, customCommentStart));
                                }
                            }
                            else
                            {
                                result.Append(line + Environment.NewLine);
                                state = CommentState.None;
                            }
                        }
                    }
                    else if (singleLineCommentStart != -1)
                    {
                        state = CommentState.SingleLine;
                    }
                    else
                    {
                        result.Append(line + Environment.NewLine);
                    }
                }
                if (state == CommentState.SingleLine)
                {
                    if (!IsCompleteComment(line, singleLineCommentStart))
                    {
                        if (line.Length > 1)
                        {
                            result.Append(line.Substring(0, singleLineCommentStart));
                        }
                        state = CommentState.None;
                    }
                    else
                    {
                        result.Append(line + Environment.NewLine);
                    }
                }
                if (state != CommentState.MultiLine)
                {
                    continue;
                }
                int customCommentEnd = line.IndexOf("#/");
                if (customCommentEnd != -1)
                {
                    customCommentEnd += 2;
                    if (customCommentEnd > line.Length)
                    {
                        result.Append(line.Substring(customCommentEnd, line.Length - 1));
                    }
                    state = CommentState.None;
                }
            }
            reader.Close();
            return result.ToString();
        }

        // Helper method to check if the comment is complete in the same line
        private static bool IsCompleteComment(string line, int commentStartIndex)
        {
            bool insideString = false;
            int index = 0;
            while (true)
            {
                if (index < line.Length)
                {
                    if (line[index] == '"')
                    {
                        insideString = !insideString;
                    }
                    if (!insideString && index == commentStartIndex)
                    {
                        break;
                    }
                    index++;
                    continue;
                }
                return true;
            }
            return false;
        }
    }
}