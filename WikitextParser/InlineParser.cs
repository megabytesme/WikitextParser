using WikitextParser.Elements;
using System.Collections.Generic;
using System.Linq;

namespace WikitextParser
{
    /// <summary>
    /// Handles parsing of inline wikitext elements (bold, italic, links, etc.).
    /// </summary>
    internal static class InlineParser
    {
        internal static IEnumerable<WikitextElement> ParseInline(string sourceText)
        {
            if (string.IsNullOrEmpty(sourceText)) yield break;

            int lastIndex = 0;
            for (int i = 0; i < sourceText.Length;)
            {
                int tokenStart = i;
                WikitextElement element;
                int nextIndex;
                if (TryParseElement(sourceText, ref i, out element, out nextIndex))
                {
                    if (tokenStart > lastIndex)
                    {
                        yield return new TextElement(sourceText.Substring(lastIndex, tokenStart - lastIndex));
                    }
                    yield return element;
                    i = nextIndex;
                    lastIndex = nextIndex;
                }
                else
                {
                    i++;
                }
            }

            if (lastIndex < sourceText.Length)
            {
                yield return new TextElement(sourceText.Substring(lastIndex));
            }
        }

        private static bool TryParseElement(string sourceText, ref int i, out WikitextElement element, out int nextIndex)
        {
            element = null;
            nextIndex = i;

            if (TryParseHtmlComment(sourceText, ref i, out element, out nextIndex)) return true;
            if (TryParseBoldItalic(sourceText, ref i, out element, out nextIndex)) return true;
            if (TryParseBold(sourceText, ref i, out element, out nextIndex)) return true;
            if (TryParseItalic(sourceText, ref i, out element, out nextIndex)) return true;
            if (BlockParser.TryParseFile(sourceText, ref i, out element, out nextIndex)) return true;
            if (TryParseLink(sourceText, ref i, out element, out nextIndex)) return true;
            if (TryParseRef(sourceText, ref i, out element, out nextIndex)) return true;
            if (BlockParser.TryParseTemplate(sourceText, ref i, out element, out nextIndex)) return true;

            return false;
        }

        private static bool TryParseHtmlComment(string sourceText, ref int i, out WikitextElement element, out int nextIndex)
        {
            element = null;
            nextIndex = i;
            if (i + 3 < sourceText.Length && sourceText.Substring(i, 4) == "<!--")
            {
                int end = sourceText.IndexOf("-->", i + 4, System.StringComparison.Ordinal);
                if (end != -1)
                {
                    nextIndex = end + 3;
                    element = new HtmlCommentElement(sourceText.Substring(i, nextIndex - i));
                    return true;
                }
            }
            return false;
        }

        private static bool TryParseBoldItalic(string sourceText, ref int i, out WikitextElement element, out int nextIndex)
        {
            element = null;
            nextIndex = i;
            if (i + 4 < sourceText.Length && sourceText.Substring(i, 5) == "'''''")
            {
                int end = sourceText.IndexOf("'''''", i + 5, System.StringComparison.Ordinal);
                if (end != -1)
                {
                    string innerSource = sourceText.Substring(i + 5, end - (i + 5));
                    BoldElement bold = new BoldElement(innerSource, ParseInline(innerSource).FirstOrDefault() ?? new TextElement(innerSource));
                    element = new ItalicElement(sourceText.Substring(i, end - i + 5), bold);
                    nextIndex = end + 5;
                    return true;
                }
            }
            return false;
        }

        private static bool TryParseBold(string sourceText, ref int i, out WikitextElement element, out int nextIndex)
        {
            element = null;
            nextIndex = i;
            if (i + 2 < sourceText.Length && sourceText.Substring(i, 3) == "'''")
            {
                int end = sourceText.IndexOf("'''", i + 3, System.StringComparison.Ordinal);
                if (end != -1)
                {
                    string innerSource = sourceText.Substring(i + 3, end - (i + 3));
                    element = new BoldElement(sourceText.Substring(i, end - i + 3), ParseInline(innerSource).FirstOrDefault() ?? new TextElement(innerSource));
                    nextIndex = end + 3;
                    return true;
                }
            }
            return false;
        }

        private static bool TryParseItalic(string sourceText, ref int i, out WikitextElement element, out int nextIndex)
        {
            element = null;
            nextIndex = i;
            if (i + 1 < sourceText.Length && sourceText.Substring(i, 2) == "''")
            {
                int end = sourceText.IndexOf("''", i + 2, System.StringComparison.Ordinal);
                if (end != -1)
                {
                    string innerSource = sourceText.Substring(i + 2, end - (i + 2));
                    element = new ItalicElement(sourceText.Substring(i, end - i + 2), ParseInline(innerSource).FirstOrDefault() ?? new TextElement(innerSource));
                    nextIndex = end + 2;
                    return true;
                }
            }
            return false;
        }

        private static bool TryParseLink(string sourceText, ref int i, out WikitextElement element, out int nextIndex)
        {
            element = null;
            nextIndex = i;
            if (i + 1 < sourceText.Length && sourceText.Substring(i, 2) == "[[")
            {
                if (sourceText.Length > i + 10 && sourceText.Substring(i + 2).StartsWith("Category:", System.StringComparison.OrdinalIgnoreCase)) return false;
                if (sourceText.Length > i + 7 && sourceText.Substring(i + 2).StartsWith("File:", System.StringComparison.OrdinalIgnoreCase)) return false;
                if (sourceText.Length > i + 8 && sourceText.Substring(i + 2).StartsWith("Image:", System.StringComparison.OrdinalIgnoreCase)) return false;

                int end = sourceText.IndexOf("]]", i + 2, System.StringComparison.Ordinal);
                if (end != -1)
                {
                    string linkContent = sourceText.Substring(i + 2, end - (i + 2));
                    string url, displayText;
                    int pipe = linkContent.IndexOf('|');
                    if (pipe != -1)
                    {
                        displayText = linkContent.Substring(0, pipe);
                        url = linkContent.Substring(pipe + 1);
                    }
                    else
                    {
                        url = displayText = linkContent;
                    }
                    element = new LinkElement(sourceText.Substring(i, end - i + 2), displayText, url);
                    nextIndex = end + 2;
                    return true;
                }
            }
            return false;
        }

        private static bool TryParseRef(string sourceText, ref int i, out WikitextElement element, out int nextIndex)
        {
            element = null;
            nextIndex = i;

            if (sourceText.Length <= i + 4 || !sourceText.Substring(i).StartsWith("<ref")) return false;

            int tagEnd = sourceText.IndexOf('>', i + 4);
            if (tagEnd == -1) return false;

            string tagContent = sourceText.Substring(i + 4, tagEnd - (i + 4));
            string name = null;

            int nameIndex = tagContent.IndexOf("name=", System.StringComparison.OrdinalIgnoreCase);
            if (nameIndex != -1)
            {
                int valueStart = nameIndex + 5;
                char quoteChar = tagContent[valueStart];
                if (quoteChar == '"' || quoteChar == '\'')
                {
                    int valueEnd = tagContent.IndexOf(quoteChar, valueStart + 1);
                    if (valueEnd != -1)
                    {
                        name = tagContent.Substring(valueStart + 1, valueEnd - (valueStart + 1));
                    }
                }
            }

            if (tagContent.Trim().EndsWith("/"))
            {
                nextIndex = tagEnd + 1;
                string fullSource = sourceText.Substring(i, nextIndex - i);
                element = new RefElement(fullSource, null, name);
                return true;
            }

            int contentEnd = sourceText.IndexOf("</ref>", tagEnd, System.StringComparison.Ordinal);
            if (contentEnd != -1)
            {
                nextIndex = contentEnd + 6;
                string innerSource = sourceText.Substring(tagEnd + 1, contentEnd - (tagEnd + 1));
                WikitextElement childElement = null;

                if (!string.IsNullOrEmpty(innerSource))
                {
                    var children = ParseInline(innerSource).ToList();
                    childElement = children.Count == 1 ? children.First() : new ParagraphElement(innerSource, children.Select(c => c));
                }

                string fullSource = sourceText.Substring(i, nextIndex - i);
                element = new RefElement(fullSource, childElement, name);
                return true;
            }

            return false;
        }
    }
}