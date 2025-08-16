using WikitextParser.Elements;

namespace WikitextParser;

public static class Parser
{
    public static IEnumerable<WikitextElement> Parse(string sourceText)
    {
        sourceText = sourceText.ReplaceLineEndings("\n");
        int currentIndex = 0;

        while (currentIndex < sourceText.Length)
        {
            while (currentIndex < sourceText.Length && char.IsWhiteSpace(sourceText[currentIndex]))
            {
                currentIndex++;
            }
            if (currentIndex >= sourceText.Length) break;

            if (sourceText.Substring(currentIndex).StartsWith("{{"))
            {
                int end = FindMatchingDelimiters(sourceText, currentIndex, "{{", "}}");
                if (end != -1)
                {
                    string elementSource = sourceText.Substring(currentIndex, end - currentIndex);
                    string innerText = elementSource.Substring(2, elementSource.Length - 4).Trim();

                    if (!innerText.Contains('\n'))
                    {
                        string[] parts = innerText.Split(['|'], 2);
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim();
                            string valueSource = parts[1].Trim();
                            WikitextElement valueElement = ParseInline(valueSource).FirstOrDefault() ?? new TextElement(valueSource);

                            yield return new WikiKeyValuePairElement(elementSource, key, valueElement);
                            currentIndex = end;
                            continue;
                        }
                    }

                    int pipeIndex = innerText.IndexOfAny(['|', '\n']);
                    string templateName = (pipeIndex != -1) ? innerText.Substring(0, pipeIndex).Trim() : innerText.Trim();

                    yield return new TemplateElement(elementSource) { TemplateName = templateName };
                    currentIndex = end;
                    continue;
                }
            }

            int nextTemplateStart = sourceText.IndexOf("{{", currentIndex, StringComparison.Ordinal);
            int endOfParagraph = (nextTemplateStart != -1) ? nextTemplateStart : sourceText.Length;

            string paragraphSource = sourceText.Substring(currentIndex, endOfParagraph - currentIndex).Trim();
            if (!string.IsNullOrEmpty(paragraphSource))
            {
                IEnumerable<WikitextElement?> elements = ParseInline(paragraphSource).ToArray();

                if (elements.Any(x => x is null))
                {
                    throw new InvalidOperationException("Failed to parse element");
                }

                yield return new ParagraphElement(paragraphSource, ParseInline(paragraphSource).Select(x => x!));
            }

            currentIndex = endOfParagraph;
        }
    }

    internal static IEnumerable<WikitextElement?> ParsePlainList(TemplateElement template)
    {
        string innerText = template.SourceText.Substring(2, template.SourceText.Length - 4);

        int nameEndIndex = innerText.IndexOf('|');
        if (nameEndIndex == -1) yield break;
        string listText = innerText.Substring(nameEndIndex + 1);

        string[] items = listText.Split(['\n'], StringSplitOptions.RemoveEmptyEntries);

        foreach (string item in items)
        {
            string trimmedItem = item.Trim();
            if (trimmedItem.StartsWith("*"))
            {
                string itemContent = trimmedItem.Substring(1).Trim();
                foreach (WikitextElement? element in ParseInline(itemContent))
                {
                    yield return element;
                }
            }
        }
    }

    internal static IEnumerable<WikiKeyValuePairElement> ParseTemplateKeyValuePairs(TemplateElement template)
    {
        string innerText = template.SourceText.Substring(2, template.SourceText.Length - 4);

        int nameEndIndex = innerText.IndexOf('|');
        if (nameEndIndex == -1) yield break;

        string paramsText = innerText.Substring(nameEndIndex + 1);

        string[] parameters = paramsText.Split(["\n|"], StringSplitOptions.None);

        foreach (string parameter in parameters)
        {
            string trimmedParam = parameter.Trim();
            if (string.IsNullOrEmpty(trimmedParam)) continue;

            string[] parts = trimmedParam.Split(['='], 2);
            if (parts.Length == 2)
            {
                string key = parts[0].Trim();
                string valueSource = parts[1].Trim();

                List<WikitextElement?> parsedValueElements = ParseInline(valueSource).ToList();

                if (parsedValueElements.Any(x => x is null))
                {
                    throw new InvalidOperationException("Failed to parse inline element");
                }

                WikitextElement valueElement;

                if (parsedValueElements.Count == 1)
                {
                    valueElement = parsedValueElements.First()!;
                }
                else if (parsedValueElements.Any())
                {
                    valueElement = new ParagraphElement(valueSource, parsedValueElements.Select(e => e!));
                }
                else
                {
                    valueElement = new TextElement(valueSource);
                }

                yield return new WikiKeyValuePairElement(trimmedParam, key, valueElement);
            }
        }
    }

    private static IEnumerable<WikitextElement?> ParseInline(string sourceText)
    {
        if (string.IsNullOrEmpty(sourceText)) yield break;

        int lastIndex = 0;
        for (int i = 0; i < sourceText.Length;)
        {
            int tokenStart = i;
            WikitextElement? element;
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

    private static bool TryParseElement(string sourceText, ref int i, out WikitextElement? element, out int nextIndex)
    {
        element = null;
        nextIndex = i;

        if (TryParseHtmlComment(sourceText, ref i, out element, out nextIndex)) return true;
        if (TryParseBoldItalic(sourceText, ref i, out element, out nextIndex)) return true;
        if (TryParseBold(sourceText, ref i, out element, out nextIndex)) return true;
        if (TryParseItalic(sourceText, ref i, out element, out nextIndex)) return true;
        if (TryParseLink(sourceText, ref i, out element, out nextIndex)) return true;
        if (TryParseRef(sourceText, ref i, out element, out nextIndex)) return true;
        if (TryParseTemplate(sourceText, ref i, out element, out nextIndex)) return true;

        return false;
    }

    private static bool TryParseHtmlComment(string sourceText, ref int i, out WikitextElement? element, out int nextIndex)
    {
        element = null;
        nextIndex = i;
        if (i + 3 < sourceText.Length && sourceText.Substring(i, 4) == "<!--")
        {
            int end = sourceText.IndexOf("-->", i + 4, StringComparison.Ordinal);
            if (end != -1)
            {
                nextIndex = end + 3;
                element = new HtmlCommentElement(sourceText.Substring(i, nextIndex - i));
                return true;
            }
        }
        return false;
    }

    private static bool TryParseBoldItalic(string sourceText, ref int i, out WikitextElement? element, out int nextIndex)
    {
        element = null;
        nextIndex = i;
        if (i + 4 < sourceText.Length && sourceText.Substring(i, 5) == "'''''")
        {
            int end = sourceText.IndexOf("'''''", i + 5, StringComparison.Ordinal);
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

    private static bool TryParseBold(string sourceText, ref int i, out WikitextElement? element, out int nextIndex)
    {
        element = null;
        nextIndex = i;
        if (i + 2 < sourceText.Length && sourceText.Substring(i, 3) == "'''")
        {
            int end = sourceText.IndexOf("'''", i + 3, StringComparison.Ordinal);
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

    private static bool TryParseItalic(string sourceText, ref int i, out WikitextElement? element, out int nextIndex)
    {
        element = null;
        nextIndex = i;
        if (i + 1 < sourceText.Length && sourceText.Substring(i, 2) == "''")
        {
            int end = sourceText.IndexOf("''", i + 2, StringComparison.Ordinal);
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

    private static bool TryParseLink(string sourceText, ref int i, out WikitextElement? element, out int nextIndex)
    {
        element = null;
        nextIndex = i;
        if (i + 1 < sourceText.Length && sourceText.Substring(i, 2) == "[[")
        {
            int end = sourceText.IndexOf("]]", i + 2, StringComparison.Ordinal);
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

    private static bool TryParseRef(string sourceText, ref int i, out WikitextElement? element, out int nextIndex)
    {
        element = null;
        nextIndex = i;
        if (i + 4 < sourceText.Length && sourceText.Substring(i).StartsWith("<ref>"))
        {
            int end = sourceText.IndexOf("</ref>", i + 5, StringComparison.Ordinal);
            if (end != -1)
            {
                string innerSource = sourceText.Substring(i + 5, end - (i + 5));
                element = new RefElement(sourceText.Substring(i, end - i + 6), ParseInline(innerSource).FirstOrDefault() ?? new TextElement(innerSource));
                nextIndex = end + 6;
                return true;
            }
        }
        return false;
    }

    private static bool TryParseTemplate(string sourceText, ref int i, out WikitextElement? element, out int nextIndex)
    {
        element = null;
        nextIndex = i;
        if (i + 1 < sourceText.Length && sourceText.Substring(i, 2) == "{{")
        {
            int end = FindMatchingDelimiters(sourceText, i, "{{", "}}");
            if (end != -1)
            {
                string templateSource = sourceText.Substring(i, end - i);
                string innerText = templateSource.Substring(2, templateSource.Length - 4);
                int pipeIndex = innerText.IndexOfAny(['|', '\n']);
                string templateName = (pipeIndex != -1) ? innerText.Substring(0, pipeIndex).Trim() : innerText.Trim();
                element = new TemplateElement(templateSource) { TemplateName = templateName };
                nextIndex = end;
                return true;
            }
        }
        return false;
    }

    private static int FindMatchingDelimiters(string text, int startIndex, string open, string close)
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
}