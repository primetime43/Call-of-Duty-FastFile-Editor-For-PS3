using System;
using System.Collections.Generic;
using System.Text;

namespace Call_of_Duty_FastFile_Editor.IO
{
    public static class FilePatternFinder
    {
        public static List<int> FindBytePattern(byte[] data, byte[] pattern)
        {
            List<int> positions = new List<int>();
            int patternLength = pattern.Length;

            for (int i = 0; i <= data.Length - patternLength; i++)
            {
                bool found = true;

                for (int j = 0; j < patternLength; j++)
                {
                    if (data[i + j] != pattern[j])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    positions.Add(i);
                }
            }

            return positions;
        }

        /*public static string ExtractFullFileName(byte[] data, int patternPosition)
        {
            StringBuilder fileName = new StringBuilder();

            int startPosition = patternPosition;
            while (startPosition >= 4 && data[startPosition - 4] == 0xFF && data[startPosition - 3] == 0xFF && data[startPosition - 2] == 0xFF && data[startPosition - 1] == 0xFF)
            {
                startPosition -= 4;
            }

            while (startPosition > 0 && data[startPosition - 1] != 0x00)
            {
                startPosition--;
            }

            for (int i = startPosition; i < data.Length; i++)
            {
                char c = (char)data[i];
                if (c == '\0')
                {
                    break;
                }
                fileName.Append(c);
            }

            string fileNameStr = fileName.ToString();
            int invalidSequenceIndex = fileNameStr.IndexOf("ÿÿÿÿ");
            if (invalidSequenceIndex != -1)
            {
                fileNameStr = fileNameStr.Substring(invalidSequenceIndex + 4);
            }

            return fileNameStr.ToString();
        }*/

        public static int FindPattern(byte[] data, int startPosition, byte[] pattern)
        {
            for (int i = startPosition; i <= data.Length - pattern.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (data[i + j] != pattern[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    return i;
                }
            }
            return data.Length; // Return the end of the data if the pattern is not found
        }
    }
}