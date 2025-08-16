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

            if (TryParseHeading(sourceText, ref currentIndex, out var heading))
            {
                yield return heading!;
                continue;
            }

            if (sourceText.Substring(currentIndex).StartsWith("[[Category:", StringComparison.OrdinalIgnoreCase))
            {
                int tempIndex = currentIndex;
                if (TryParseCategoryLink(sourceText, ref tempIndex, out var category, out var nextIndex))
                {
                    yield return category!;
                    currentIndex = nextIndex;
                    continue;
                }
            }

            if (sourceText.Substring(currentIndex).StartsWith("{{"))
            {
                int end = FindMatchingDelimiters(sourceText, currentIndex, "{{", "}}");
                if (end != -1)
                {
                    string elementSource = sourceText.Substring(currentIndex, end - currentIndex);
                    string innerText = elementSource.Substring(2, elementSource.Length - 4).Trim();

                    if (!innerText.Contains('\n'))
                    {
                        var parts = SplitAtTopLevel(innerText, '|').ToList();
                        if (parts.Count == 2)
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
                    string templateName = pipeIndex != -1 ? innerText.Substring(0, pipeIndex).Trim() : innerText.Trim();

                    yield return new TemplateElement(elementSource) { TemplateName = templateName };
                    currentIndex = end;
                    continue;
                }
            }

            int endOfParagraph = sourceText.Length;

            int nextTemplateStart = sourceText.IndexOf("{{", currentIndex);
            if (nextTemplateStart != -1)
            {
                endOfParagraph = nextTemplateStart;
            }

            int nextHeadingStart = sourceText.IndexOf("\n=", currentIndex);
            if (nextHeadingStart != -1)
            {
                endOfParagraph = Math.Min(endOfParagraph, nextHeadingStart + 1);
            }

            string paragraphSource = sourceText.Substring(currentIndex, endOfParagraph - currentIndex).Trim();
            if (!string.IsNullOrEmpty(paragraphSource))
            {
                yield return new ParagraphElement(paragraphSource, ParseInline(paragraphSource).Select(x => x!));
            }

            currentIndex = endOfParagraph;
        }
    }

    private static bool TryParseHeading(string text, ref int currentIndex, out HeadingElement? heading)
    {
        heading = null;
        int i = currentIndex;

        if (i > 0 && text[i - 1] != '\n') return false;

        int level = 0;
        while (i < text.Length && text[i] == '=')
        {
            level++;
            i++;
        }

        if (level < 2) return false;

        int endOfLine = text.IndexOf('\n', i);
        if (endOfLine == -1)
        {
            endOfLine = text.Length;
        }

        string lineContent = text.Substring(i, endOfLine - i);
        string innerText = lineContent.TrimEnd('=').Trim();

        if (string.IsNullOrEmpty(innerText)) return false;

        string sourceText = text.Substring(currentIndex, endOfLine - currentIndex);
        var childElement = ParseInline(innerText).FirstOrDefault() ?? new TextElement(innerText);

        heading = new HeadingElement(sourceText, level, childElement);
        currentIndex = endOfLine;
        return true;
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

    private static IEnumerable<string> SplitAtTopLevel(string text, char separator)
    {
        int level = 0;
        int lastSplit = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (i + 1 < text.Length)
            {
                if (text.Substring(i, 2) is "{{" or "[[")
                {
                    level++;
                    i++;
                    continue;
                }
                if (text.Substring(i, 2) is "}}" or "]]")
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

    private static bool TryParseCategoryLink(string sourceText, ref int i, out WikitextElement? element, out int nextIndex)
    {
        element = null;
        nextIndex = i;

        const string prefix = "[[Category:";
        if (sourceText.Substring(i).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            int end = sourceText.IndexOf("]]", i + prefix.Length, StringComparison.Ordinal);
            if (end != -1)
            {
                string fullSource = sourceText.Substring(i, end - i + 2);
                string content = fullSource.Substring(prefix.Length, fullSource.Length - prefix.Length - 2);

                string categoryName;
                string? sortKey = null;

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
            if (sourceText.Length > i + 10 && sourceText.Substring(i + 2).StartsWith("Category:", StringComparison.OrdinalIgnoreCase)) return false;
            if (sourceText.Length > i + 7 && sourceText.Substring(i + 2).StartsWith("File:", StringComparison.OrdinalIgnoreCase)) return false;
            if (sourceText.Length > i + 8 && sourceText.Substring(i + 2).StartsWith("Image:", StringComparison.OrdinalIgnoreCase)) return false;

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

    internal static IEnumerable<TemplateParameterElement> ParseTemplateParameters(TemplateElement template)
    {
        string innerText = template.SourceText.Substring(2, template.SourceText.Length - 4);

        int firstPipe = innerText.IndexOf('|');
        if (firstPipe == -1) yield break; // No parameters

        string paramsText = innerText.Substring(firstPipe + 1);

        var parameterStrings = SplitAtTopLevel(paramsText, '|');

        foreach (string paramStr in parameterStrings)
        {
            string trimmedParam = paramStr.Trim();
            if (string.IsNullOrEmpty(trimmedParam)) continue;

            string? key = null;
            string valueSource;

            int equalsIndex = trimmedParam.IndexOf('=');
            if (equalsIndex > 0)
            {
                key = trimmedParam.Substring(0, equalsIndex).Trim();
                valueSource = trimmedParam.Substring(equalsIndex + 1).Trim();
            }
            else
            {
                valueSource = trimmedParam;
            }
            
            var parsedValueElements = ParseInline(valueSource).ToList();
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

            yield return new TemplateParameterElement(trimmedParam, key, valueElement);
        }
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