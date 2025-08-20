using System.Collections.Generic;

namespace WikitextParser
{
    /// <summary>
    /// Internal utility methods for the parser.
    /// </summary>
    internal static class ParserUtils
    {
        internal static int FindMatchingDelimiters(string text, int startIndex, string open, string close)
        {
            int count = 0;
            for (int i = startIndex; i < text.Length; i++)
            {
                if (i + open.Length <= text.Length && text.Substring(i, open.Length) == open)
                {
                    count++;
                    i += open.Length - 1;
                }
                else if (i + close.Length <= text.Length && text.Substring(i, close.Length) == close)
                {
                    count--;
                    if (count == 0)
                    {
                        return i + close.Length;
                    }
                    i += close.Length - 1;
                }
            }
            return -1;
        }

        internal static IEnumerable<string> SplitAtTopLevel(string text, char separator)
        {
            int level = 0;
            int lastSplit = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (i + 1 < text.Length)
                {
                    if (text.Substring(i, 2) is "{{")
                    {
                        level++;
                        i++;
                        continue;
                    } else if (text.Substring(i, 2) is "[[")
                    {
                        level++;
                        i++;
                        continue;
                    } else if (text.Substring(i, 2) is "}}")
                    {
                        level--;
                        i++;
                        continue;
                    } else if (text.Substring(i, 2) is "]]")
                    {
                        level--;
                        i++;
                        continue;
                    }
                }
                if (text[i] == separator && level == 0)
                {
                    yield return text.Substring(lastSplit, i - lastSplit);
                    lastSplit = i + 1;
                }
            }
            yield return text.Substring(lastSplit);
        }
    }
}