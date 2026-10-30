using System;
using System.Collections.Generic;
using System.Linq;
using WikitextParser.Elements;

namespace WikitextParser
{
    internal static class ListParser
    {
        private sealed class Line { internal string Source, Prefix, Content; }
        internal static bool IsMarker(char c) => c == '*' || c == '#' || c == ';' || c == ':';
        private static char Kind(char c) => c == ';' ? ':' : c;

        internal static WikitextElement Parse(string text, ref int index, ParseContext context = null)
        {
            int start = index;
            var lines = new List<Line>();
            while (index < text.Length && IsMarker(text[index]))
            {
                int end = text.IndexOf('\n', index);
                if (end < 0) end = text.Length;
                int prefixEnd = index;
                while (prefixEnd < end && IsMarker(text[prefixEnd]) && prefixEnd - index < 128) prefixEnd++;
                string prefix = text.Substring(index, prefixEnd - index), content = text.Substring(prefixEnd, end - prefixEnd).Trim();
                string source = text.Substring(index, end - index);
                // A same-line definition belongs to the same dl as its term.
                var parts = prefix.EndsWith(";", StringComparison.Ordinal) ? ParserUtils.SplitAtTopLevel(content, ':').ToList() : null;
                if (parts != null && parts.Count > 1)
                {
                    lines.Add(new Line { Source = source, Prefix = prefix, Content = parts[0].Trim() });
                    lines.Add(new Line { Source = "", Prefix = prefix.Substring(0, prefix.Length - 1) + ":", Content = string.Join(":", parts.Skip(1)).Trim() });
                }
                else lines.Add(new Line { Source = source, Prefix = prefix, Content = content });
                index = end < text.Length ? end + 1 : end;
            }
            int position = 0;
            var lists = ParseLevel(lines, ref position, 0, "", context);
            string fullSource = text.Substring(start, index - start).TrimEnd('\n');
            return lists.Count == 1 ? (WikitextElement)lists[0] : new BlockContainerElement(fullSource, lists);
        }

        private static List<ListElement> ParseLevel(List<Line> lines, ref int position, int depth, string parentPrefix, ParseContext context)
        {
            var result = new List<ListElement>();
            while (position < lines.Count && lines[position].Prefix.Length > depth && lines[position].Prefix.StartsWith(parentPrefix, StringComparison.Ordinal))
            {
                char marker = lines[position].Prefix[depth];
                var items = new List<ListItemElement>();
                int groupStart = position;
                while (position < lines.Count && lines[position].Prefix.Length > depth &&
                    lines[position].Prefix.StartsWith(parentPrefix, StringComparison.Ordinal) && Kind(lines[position].Prefix[depth]) == Kind(marker))
                {
                    var line = lines[position];
                    string prefix = line.Prefix.Substring(0, depth + 1);
                    bool explicitItem = line.Prefix.Length == depth + 1;
                    if (explicitItem) position++;
                    var nested = ParseLevel(lines, ref position, depth + 1, prefix, context);
                    items.Add(new ListItemElement(explicitItem ? line.Source : "", prefix,
                        InlineParser.Content(explicitItem ? line.Content : "", depth, context), nested));
                }
                result.Add(new ListElement(string.Join("\n", lines.Skip(groupStart).Take(position - groupStart).Select(l => l.Source)), marker, items));
            }
            return result;
        }
    }
}
