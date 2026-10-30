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
        internal static bool TryParseHeading(string text, ref int currentIndex, out HeadingElement heading, ParseContext context = null)
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

            if (level == 0) return false;

            int endOfLine = text.IndexOf('\n', i);
            if (endOfLine == -1) endOfLine = text.Length;

            int trimmedEnd = endOfLine;
            while (trimmedEnd > i && (text[trimmedEnd - 1] == ' ' || text[trimmedEnd - 1] == '\t')) trimmedEnd--;
            int closing = 0;
            while (trimmedEnd - closing > i && text[trimmedEnd - closing - 1] == '=') closing++;
            if (closing == 0) return false;
            level = System.Math.Min(6, System.Math.Min(level, closing));
            string innerText = text.Substring(currentIndex + level, trimmedEnd - currentIndex - 2 * level).Trim();

            if (string.IsNullOrEmpty(innerText)) return false;

            string sourceText = text.Substring(currentIndex, endOfLine - currentIndex);
            var childElement = InlineParser.Content(innerText, 0, context);

            heading = new HeadingElement(sourceText, level, childElement);
            currentIndex = endOfLine;
            return true;
        }

        internal static bool TryParseCategoryLink(string sourceText, ref int i, out WikitextElement element, out int nextIndex, ParseContext context = null)
        {
            element = null;
            nextIndex = i;

            context = context ?? ParseContext.Default;
            int prefixLength = context.NamespaceLength(sourceText, i, context.CategoryNamespaces) + 2;
            if (prefixLength > 2)
            {
                int end = ParserUtils.FindMatchingDelimiters(sourceText, i, "[[", "]]");
                if (end != -1)
                {
                    string fullSource = sourceText.Substring(i, end - i);
                    string content = fullSource.Substring(prefixLength, fullSource.Length - prefixLength - 2);

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
                    nextIndex = end;
                    return true;
                }
            }
            return false;
        }

        internal static bool TryParseTemplate(string sourceText, ref int i, out WikitextElement element, out int nextIndex, ParseContext context = null, int depth = 0)
        {
            element = null;
            nextIndex = i;
            if (ParserUtils.StartsAt(sourceText, i, "{{") && !ParserUtils.StartsAt(sourceText, i, "{{{"))
            {
                int end = ParserUtils.FindMatchingDelimiters(sourceText, i, "{{", "}}");
                if (end != -1)
                {
                    string templateSource = sourceText.Substring(i, end - i);
                    string innerText = templateSource.Substring(2, templateSource.Length - 4);
                    string templateName = ParserUtils.SplitAtTopLevel(innerText, '|').First().Trim();
                    element = new TemplateElement(templateSource, context ?? ParseContext.Default, depth) { TemplateName = templateName };
                    nextIndex = end;
                    return true;
                }
            }
            return false;
        }

        internal static bool TryParseFile(string sourceText, ref int i, out WikitextElement element, out int nextIndex, ParseContext context = null)
        {
            element = null;
            nextIndex = i;

            if (sourceText.Length <= i + 1 || sourceText.Substring(i, 2) != "[[") return false;

            context = context ?? ParseContext.Default;
            int prefixLength = context.NamespaceLength(sourceText, i, context.FileNamespaces);
            if (prefixLength == 0) return false;

            int end = ParserUtils.FindMatchingDelimiters(sourceText, i, "[[", "]]");
            if (end == -1) return false;

            nextIndex = end;
            string fullSource = sourceText.Substring(i, nextIndex - i);
            string innerContent = fullSource.Substring(2 + prefixLength, fullSource.Length - (4 + prefixLength));

            var parts = ParserUtils.SplitAtTopLevel(innerContent, '|').ToArray();
            var fileName = parts[0].Trim();
            var options = new List<string>();
            string caption = null;

            var knownOptions = new HashSet<string> { "thumb", "thumbnail", "frame", "framed", "frameless", "border", "right", "left", "center", "none", "baseline", "middle", "sub", "super", "top", "text-top", "bottom", "text-bottom" };

            int temp;
            for (int p = 1; p < parts.Length; p++)
            {
                var part = parts[p].Trim();
                string canonical;
                if (context.ImageOptions.TryGetValue(part, out canonical)) part = canonical;
                var partLower = part.ToLowerInvariant();

                if (partLower.EndsWith("px") && int.TryParse(partLower.Replace("px", "").TrimStart('x'), out temp))
                    options.Add(part);
                else if (knownOptions.Contains(partLower))
                    options.Add(part);
                else if (partLower.StartsWith("alt=") || partLower.StartsWith("link=") || partLower.StartsWith("upright") || partLower.StartsWith("page=") || partLower.StartsWith("class=") || System.Text.RegularExpressions.Regex.IsMatch(partLower, @"^(\d+)?x?\d+px$"))
                    options.Add(part);
                else
                    caption = part;
            }

            element = new ImageElement(fullSource, fileName, options, caption);
            return true;
        }
    }
}
