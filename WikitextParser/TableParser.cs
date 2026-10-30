using System;
using System.Collections.Generic;
using System.Linq;
using WikitextParser.Elements;

namespace WikitextParser
{
    internal static class TableParser
    {
        private sealed class Cell { internal string Source; internal bool Header; }

        internal static bool TryParse(string text, ref int index, out TableElement table, int depth = 0, ParseContext context = null)
        {
            table = null;
            if (depth >= 128 || !ParserUtils.StartsAt(text, index, "{|")) return false;
            int end = ParserUtils.FindMatchingDelimiters(text, index, "{|", "|}");
            if (end < 0) return false;
            string source = text.Substring(index, end - index), inner = source.Substring(2, source.Length - 4);
            var lines = ParserUtils.SplitAtTopLevel(inner, '\n').ToList();
            string attributes = lines[0].Trim(), rowAttributes = "", captionAttributes = "";
            WikitextElement caption = null;
            var rows = new List<TableRowElement>();
            var cells = new List<Cell>();
            foreach (string rawLine in lines.Skip(1))
            {
                string line = rawLine.TrimEnd('\r');
                if (line.StartsWith("|+", StringComparison.Ordinal))
                {
                    var parsed = ParseCell(line.Substring(2), false, depth, context);
                    caption = parsed.Content;
                    captionAttributes = parsed.Attributes;
                }
                else if (line.StartsWith("|-", StringComparison.Ordinal))
                {
                    AddRow(rows, cells, rowAttributes, depth, context);
                    cells.Clear();
                    rowAttributes = line.Substring(2).Trim();
                }
                else if (line.StartsWith("|", StringComparison.Ordinal) || line.StartsWith("!", StringComparison.Ordinal))
                {
                    bool header = line[0] == '!';
                    foreach (string cell in ParserUtils.SplitAtTopLevel(line.Substring(1), header ? "!!" : "||", true))
                        cells.Add(new Cell { Source = cell, Header = header });
                }
                else if (cells.Count > 0) cells[cells.Count - 1].Source += "\n" + line;
            }
            AddRow(rows, cells, rowAttributes, depth, context);
            table = new TableElement(source, attributes, rows, caption, captionAttributes);
            index = end;
            return true;
        }

        private static void AddRow(List<TableRowElement> rows, List<Cell> cells, string attributes, int depth, ParseContext context)
        {
            if (cells.Count > 0) rows.Add(new TableRowElement(string.Join("\n", cells.Select(c => c.Source)), attributes,
                cells.Select(c => ParseCell(c.Source, c.Header, depth, context))));
        }

        private static TableCellElement ParseCell(string source, bool header, int depth, ParseContext context)
        {
            int pipe = ParserUtils.TopLevelSeparators(source, "|", true).DefaultIfEmpty(-1).First();
            string attributes = "", content = source;
            if (pipe >= 0 && HtmlAttributes.Parse(source.Substring(0, pipe)).Any())
            { attributes = source.Substring(0, pipe).Trim(); content = source.Substring(pipe + 1); }
            content = content.Trim();
            var blocks = DocumentParser.Parse(content, depth + 1, context).ToList();
            WikitextElement value;
            if (blocks.Count == 1 && blocks[0] is ParagraphElement paragraph)
            {
                var children = paragraph.ChildElements.ToList();
                value = children.Count == 1 ? children[0] : new InlineContainerElement(content, children);
            }
            else value = blocks.Count == 1 ? blocks[0] : new BlockContainerElement(content, blocks);
            return new TableCellElement(source, attributes, value, header);
        }
    }
}
