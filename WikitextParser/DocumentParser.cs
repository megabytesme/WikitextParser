using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WikitextParser.Elements;

namespace WikitextParser
{
    internal static class DocumentParser
    {
        private static readonly Regex Rule = new Regex(@"^-{4,}[ \t]*$");
        internal static IEnumerable<WikitextElement> Parse(string text, int depth = 0, ParseContext context = null)
        {
            context = context ?? ParseContext.Default;
            if (text == null) throw new ArgumentNullException(nameof(text));
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");
            if (depth >= 128) { yield return new TextElement(text); yield break; }
            int index = 0;
            while (index < text.Length)
            {
                int lineEnd = EndOfLine(text, index);
                string line = text.Substring(index, lineEnd - index);
                if (string.IsNullOrWhiteSpace(line)) { index = lineEnd < text.Length ? lineEnd + 1 : lineEnd; continue; }
                bool lineStart = index == 0 || text[index - 1] == '\n';
                int redirectLength = context.RedirectLength(line);
                if (index == 0 && redirectLength > 0)
                {
                    var target = InlineParser.ParseInline(line.Substring(redirectLength).Trim(), depth, context).OfType<LinkElement>().FirstOrDefault();
                    if (target != null) { yield return new RedirectElement(line, target); index = lineEnd; continue; }
                }
                if (lineStart && text[index] == ' ')
                {
                    int start = index;
                    var content = new StringBuilder();
                    while (index < text.Length && text[index] == ' ')
                    {
                        lineEnd = EndOfLine(text, index);
                        if (content.Length > 0) content.Append('\n');
                        content.Append(text.Substring(index + 1, lineEnd - index - 1));
                        index = lineEnd < text.Length ? lineEnd + 1 : lineEnd;
                    }
                    yield return new PreformattedElement(text.Substring(start, index - start).TrimEnd('\n'), InlineParser.Content(content.ToString(), depth, context));
                    continue;
                }
                if (lineStart && ListParser.IsMarker(text[index])) { yield return ListParser.Parse(text, ref index, context); continue; }
                if (lineStart && Rule.IsMatch(line)) { yield return new HorizontalRuleElement(line); index = lineEnd; continue; }
                TableElement table;
                if (lineStart && TableParser.TryParse(text, ref index, out table, depth, context)) { yield return table; continue; }
                HeadingElement heading;
                if (lineStart && BlockParser.TryParseHeading(text, ref index, out heading, context)) { yield return heading; continue; }
                if (text[index] == '<')
                {
                    TagToken tag;
                    if (TagScanner.TryRead(text, index, out tag))
                    {
                        string source = text.Substring(index, tag.End - index), body = text.Substring(tag.ContentStart, tag.ContentEnd - tag.ContentStart);
                        if (tag.Name == "pre")
                        { yield return new LiteralElement(source, body, true); index = tag.End; continue; }
                        if (tag.Name == "div" || tag.Name == "blockquote" || tag.Name == "center")
                        {
                            yield return new HtmlElement(source, tag.Name, tag.Attributes, new BlockContainerElement(body, Parse(body, depth + 1, context)), tag.SelfClosing);
                            index = tag.End; continue;
                        }
                    }
                }
                WikitextElement category;
                int next;
                if (BlockParser.TryParseCategoryLink(text, ref index, out category, out next, context))
                { yield return category; index = next; continue; }
                WikitextElement template;
                if (!ParserUtils.StartsAt(text, index, "{{{") && BlockParser.TryParseTemplate(text, ref index, out template, out next, context, depth))
                {
                    var parts = ParserUtils.SplitAtTopLevel(template.SourceText.Substring(2, template.SourceText.Length - 4), '|').ToList();
                    string name = parts[0].Trim();
                    if (parts.Count == 2 && !template.SourceText.Contains("\n") &&
                        (name.Equals("Short description", StringComparison.OrdinalIgnoreCase) || name.Equals("About", StringComparison.OrdinalIgnoreCase) ||
                         name.StartsWith("Use ", StringComparison.OrdinalIgnoreCase) || name.Equals("TOC limit", StringComparison.OrdinalIgnoreCase)))
                        yield return new WikiKeyValuePairElement(template.SourceText, name, InlineParser.Content(parts[1].Trim(), depth, context));
                    else yield return template;
                    index = next; continue;
                }
                int end = EndOfParagraph(text, index, context);
                string paragraph = text.Substring(index, end - index).TrimEnd();
                if (paragraph.Length > 0) yield return new ParagraphElement(paragraph, InlineParser.ParseInline(paragraph, depth, context));
                index = end;
            }
        }

        private static int EndOfLine(string text, int start)
        { int end = text.IndexOf('\n', start); return end < 0 ? text.Length : end; }

        private static int EndOfParagraph(string text, int start, ParseContext context)
        {
            for (int i = start; i < text.Length; i++)
            {
                if (i > start && text[i - 1] == '\n' && IsBlockStart(text, i, context)) return i;
                int end;
                if (text[i] == '<')
                {
                    if (TagScanner.TrySkipLiteral(text, i, out end)) { i = end - 1; continue; }
                    TagToken tag;
                    if (TagScanner.TryRead(text, i, out tag)) { i = tag.End - 1; continue; }
                }
                string open = ParserUtils.StartsAt(text, i, "{{") ? "{{" : ParserUtils.StartsAt(text, i, "[[") ? "[[" : null;
                if (open != null)
                {
                    end = ParserUtils.FindMatchingDelimiters(text, i, open, open == "{{" ? "}}" : "]]");
                    if (end > i) { i = end - 1; continue; }
                }
            }
            return text.Length;
        }

        private static bool IsBlockStart(string text, int index, ParseContext context)
        {
            string line = text.Substring(index, EndOfLine(text, index) - index);
            if (string.IsNullOrWhiteSpace(line) || text[index] == ' ' || ListParser.IsMarker(text[index]) || Rule.IsMatch(line)) return true;
            if (ParserUtils.StartsAt(text, index, "{|") || ParserUtils.StartsAt(text, index, "{{") ||
                context.NamespaceLength(text, index, context.CategoryNamespaces) > 0) return true;
            TagToken tag;
            if (text[index] == '<' && TagScanner.TryRead(text, index, out tag) &&
                (tag.Name == "pre" || tag.Name == "div" || tag.Name == "blockquote" || tag.Name == "center")) return true;
            int temp = index;
            HeadingElement heading;
            return BlockParser.TryParseHeading(text, ref temp, out heading, context);
        }
    }
}
