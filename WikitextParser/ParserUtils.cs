using System;
using System.Collections.Generic;

namespace WikitextParser
{
    internal static class ParserUtils
    {
        internal static bool StartsAt(string text, int index, string value) => index >= 0 &&
            index + value.Length <= text.Length && string.CompareOrdinal(text, index, value, 0, value.Length) == 0;

        private static bool Opening(string text, int index, out string open, out string close)
        {
            open = close = null;
            if (StartsAt(text, index, "{{"))
            {
                int count = 2;
                while (index + count < text.Length && text[index + count] == '{') count++;
                open = count == 3 ? "{{{" : "{{";
                close = count == 3 ? "}}}" : "}}";
            }
            else if (StartsAt(text, index, "[[")) { open = "[["; close = "]]"; }
            else if (StartsAt(text, index, "{|")) { open = "{|"; close = "|}"; }
            else if (StartsAt(text, index, "[http://") || StartsAt(text, index, "[https://") || StartsAt(text, index, "[//")) { open = "["; close = "]"; }
            return open != null;
        }

        internal static int FindMatchingDelimiters(string text, int startIndex, string open, string close)
        {
            if (!StartsAt(text, startIndex, open)) return -1;
            var stack = new Stack<string>();
            stack.Push(close);
            for (int i = startIndex + open.Length; i < text.Length;)
            {
                int end;
                if (text[i] == '<' && TagScanner.TrySkipLiteral(text, i, out end)) { i = end; continue; }
                if (StartsAt(text, i, stack.Peek()))
                {
                    i += stack.Pop().Length;
                    if (stack.Count == 0) return i;
                    continue;
                }
                string nextOpen, nextClose;
                if (Opening(text, i, out nextOpen, out nextClose)) { stack.Push(nextClose); i += nextOpen.Length; }
                else i++;
            }
            return -1;
        }

        internal static IEnumerable<int> TopLevelSeparators(string text, string separator, bool protectQuotes = false)
        {
            var stack = new Stack<string>();
            char quote = '\0';
            for (int i = 0; i < text.Length;)
            {
                int end;
                if (text[i] == '<' && TagScanner.TrySkipLiteral(text, i, out end)) { i = end; continue; }
                if (protectQuotes && stack.Count == 0 && (text[i] == '\'' || text[i] == '"'))
                {
                    if (quote == '\0') quote = text[i]; else if (quote == text[i]) quote = '\0';
                    i++; continue;
                }
                if (quote != '\0') { i++; continue; }
                if (stack.Count > 0 && StartsAt(text, i, stack.Peek())) { i += stack.Pop().Length; continue; }
                string open, close;
                if (Opening(text, i, out open, out close)) { stack.Push(close); i += open.Length; continue; }
                if (stack.Count == 0 && StartsAt(text, i, separator)) { yield return i; i += separator.Length; }
                else i++;
            }
        }

        internal static IEnumerable<string> SplitAtTopLevel(string text, char separator) => SplitAtTopLevel(text, separator.ToString());
        internal static IEnumerable<string> SplitAtTopLevel(string text, string separator, bool protectQuotes = false)
        {
            int last = 0;
            foreach (int index in TopLevelSeparators(text, separator, protectQuotes))
            { yield return text.Substring(last, index - last); last = index + separator.Length; }
            yield return text.Substring(last);
        }
    }
}
