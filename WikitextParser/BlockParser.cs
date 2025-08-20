using WikitextParser.Elements;
using System.Linq;
using System.Collections.Generic;

namespace WikitextParser
{
    /// <summary>
    /// Handles parsing of block-level wikitext elements (headings, tables, etc.).
    /// </summary>
    internal static class BlockParser
    {
        internal static bool TryParseHeading(string text, ref int currentIndex, out HeadingElement heading)
        {
            heading = null;
            int i = currentIndex;

            if (i > 0 && text[i - 1] != '\n' && text[i - 1] != '\r') return false;

            int level = 0;
            while (i < text.Length && text[i] == '=')
            {
                level++;
                i++;
            }

            if (level < 2) return false;

            int endOfLine = text.IndexOf('\n', i);
            if (endOfLine == -1) endOfLine = text.Length;

            string lineContent = text.Substring(i, endOfLine - i);
            string innerText = lineContent.TrimEnd('=').Trim();

            if (string.IsNullOrEmpty(innerText)) return false;

            string sourceText = text.Substring(currentIndex, endOfLine - currentIndex);
            var childElement = InlineParser.ParseInline(innerText).FirstOrDefault() ?? new TextElement(innerText);

            heading = new HeadingElement(sourceText, level, childElement);
            currentIndex = endOfLine;
            return true;
        }

        internal static bool TryParseTable(string sourceText, ref int i, out TableElement table)
        {
            table = null;
            if (!sourceText.Substring(i).StartsWith("{|")) return false;

            int start = i;
            int end = ParserUtils.FindMatchingDelimiters(sourceText, start, "{|", "|}");
            if (end == -1) return false;

            string tableSrc = sourceText.Substring(start, end - start);
            string innerContent = tableSrc.Substring(2, tableSrc.Length - 4).Trim();

            var lines = innerContent.Split(new[] { '\n' });
            string tableAttributes = lines[0].StartsWith("|-") ? "" : lines[0];

            var rowSources = new List<string>();
            var currentRow = new System.Text.StringBuilder();

            var contentLines = lines.Skip(string.IsNullOrEmpty(tableAttributes) ? 0 : 1);

            foreach (var line in contentLines)
            {
                if (line.StartsWith("|-"))
                {
                    if (currentRow.Length > 0) rowSources.Add(currentRow.ToString());
                    currentRow.Clear();
                    currentRow.Append(line.Substring(2).Trim());
                    currentRow.Append('\n');
                }
                else
                {
                    currentRow.AppendLine(line);
                }
            }
            if (currentRow.Length > 0) rowSources.Add(currentRow.ToString());

            var rows = rowSources.Select(ParseTableRow).ToList();

            table = new TableElement(tableSrc, tableAttributes, rows);
            i = end;
            return true;
        }

        private static TableRowElement ParseTableRow(string rowSource)
        {
            var lines = rowSource.Trim().Split(new[] { '\n' });
            string rowAttributes = lines[0].Trim();

            if (rowAttributes.StartsWith("|") || rowAttributes.StartsWith("!"))
            {
                rowAttributes = "";
            }

            var cellLines = string.IsNullOrEmpty(rowAttributes) ? lines : lines.Skip(1);
            var cells = new List<TableCellElement>();

            foreach (var line in cellLines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine)) continue;

                bool isHeader = trimmedLine.StartsWith("!");
                if (isHeader || trimmedLine.StartsWith("|"))
                {
                    var cellSeparator = isHeader ? "!!" : "||";
                    var cellParts = trimmedLine.Substring(1).Split(new[] { cellSeparator }, System.StringSplitOptions.None);
                    foreach (var cellSource in cellParts)
                    {
                        cells.Add(ParseTableCell(cellSource, isHeader));
                    }
                }
            }

            return new TableRowElement(rowSource, rowAttributes, cells);
        }

        private static TableCellElement ParseTableCell(string cellSource, bool isHeader)
        {
            string attributes = "";
            string contentSource = cellSource;

            int pipeSeparator = cellSource.IndexOf('|');
            if (pipeSeparator != -1)
            {
                string potentialAttrs = cellSource.Substring(0, pipeSeparator);
                int temp;
                if (potentialAttrs.Contains("=") || potentialAttrs.Contains(";") || int.TryParse(potentialAttrs.Trim(), out temp))
                {
                    attributes = potentialAttrs.Trim();
                    contentSource = cellSource.Substring(pipeSeparator + 1);
                }
            }

            contentSource = contentSource.Trim();
            var contentElements = InlineParser.ParseInline(contentSource).Select(e => e).ToList();

            WikitextElement content = contentElements.Count == 1
                ? contentElements[0]
                : new ParagraphElement(contentSource, contentElements);

            return new TableCellElement(cellSource, attributes, content, isHeader);
        }

        internal static bool TryParseCategoryLink(string sourceText, ref int i, out WikitextElement element, out int nextIndex)
        {
            element = null;
            nextIndex = i;

            const string prefix = "[[Category:";
            if (sourceText.Substring(i).StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
            {
                int end = sourceText.IndexOf("]]", i + prefix.Length, System.StringComparison.Ordinal);
                if (end != -1)
                {
                    string fullSource = sourceText.Substring(i, end - i + 2);
                    string content = fullSource.Substring(prefix.Length, fullSource.Length - prefix.Length - 2);

                    string categoryName;
                    string sortKey = null;

                    int pipeIndex = content.IndexOf('|');
                    if (pipeIndex != -1)
                    {
                        categoryName = content.Substring(0, pipeIndex);
                        sortKey = content.Substring(pipeIndex + 1);
                    }
                    else
                    {
                        categoryName = content;
                    }

                    element = new CategoryElement(fullSource, categoryName, sortKey);
                    nextIndex = end + 2;
                    return true;
                }
            }
            return false;
        }

        internal static bool TryParseTemplate(string sourceText, ref int i, out WikitextElement element, out int nextIndex)
        {
            element = null;
            nextIndex = i;
            if (i + 1 < sourceText.Length && sourceText.Substring(i, 2) == "{{")
            {
                int end = ParserUtils.FindMatchingDelimiters(sourceText, i, "{{", "}}");
                if (end != -1)
                {
                    string templateSource = sourceText.Substring(i, end - i);
                    string innerText = templateSource.Substring(2, templateSource.Length - 4);
                    int pipeIndex = innerText.IndexOfAny(new char[] { '|', '\n' });
                    string templateName = (pipeIndex != -1) ? innerText.Substring(0, pipeIndex).Trim() : innerText.Trim();
                    element = new TemplateElement(templateSource) { TemplateName = templateName };
                    nextIndex = end;
                    return true;
                }
            }
            return false;
        }

        internal static bool TryParseFile(string sourceText, ref int i, out WikitextElement element, out int nextIndex)
        {
            element = null;
            nextIndex = i;

            if (sourceText.Length <= i + 1 || sourceText.Substring(i, 2) != "[[") return false;

            string prefix;
            if (sourceText.Substring(i + 2).StartsWith("File:", System.StringComparison.OrdinalIgnoreCase))
                prefix = "File:";
            else if (sourceText.Substring(i + 2).StartsWith("Image:", System.StringComparison.OrdinalIgnoreCase))
                prefix = "Image:";
            else
                return false;

            int end = sourceText.IndexOf("]]", i + 2, System.StringComparison.Ordinal);
            if (end == -1) return false;

            nextIndex = end + 2;
            string fullSource = sourceText.Substring(i, nextIndex - i);
            string innerContent = fullSource.Substring(2 + prefix.Length, fullSource.Length - (4 + prefix.Length));

            var parts = innerContent.Split('|');
            var fileName = parts[0].Trim();
            var options = new List<string>();
            string caption = null;

            var knownOptions = new HashSet<string> { "thumb", "thumbnail", "frame", "framed", "frameless", "border", "right", "left", "center", "none", "baseline", "middle", "sub", "super", "top", "text-top", "bottom", "text-bottom" };

            int temp;
            for (int p = 1; p < parts.Length; p++)
            {
                var part = parts[p].Trim();
                var partLower = part.ToLower();

                if (partLower.EndsWith("px") && int.TryParse(partLower.Replace("px", "").TrimStart('x'), out temp))
                    options.Add(part);
                else if (knownOptions.Contains(partLower))
                    options.Add(part);
                else if (partLower.StartsWith("alt="))
                    options.Add(part);
                else
                    caption = part;
            }

            element = new ImageElement(fullSource, fileName, options, caption);
            return true;
        }
    }
}