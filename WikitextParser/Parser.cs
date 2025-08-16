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

            if (BlockParser.TryParseTable(sourceText, ref currentIndex, out var table))
            {
                yield return table!;
                continue;
            }
            if (BlockParser.TryParseHeading(sourceText, ref currentIndex, out var heading))
            {
                yield return heading!;
                continue;
            }
            if (BlockParser.TryParseCategoryLink(sourceText, ref currentIndex, out var category, out var nextIndex))
            {
                yield return category!;
                currentIndex = nextIndex;
                continue;
            }

            if (sourceText.Substring(currentIndex).StartsWith("{{"))
            {
                int tempIndex = currentIndex;
                if (BlockParser.TryParseTemplate(sourceText, ref tempIndex, out var templateElement, out var end))
                {
                    string innerText = templateElement!.SourceText.Substring(2, templateElement.SourceText.Length - 4).Trim();
                    if (!innerText.Contains('\n'))
                    {
                        var parts = ParserUtils.SplitAtTopLevel(innerText, '|').ToList();
                        if (parts.Count == 2)
                        {
                            string key = parts[0].Trim();
                            string valueSource = parts[1].Trim();
                            WikitextElement valueElement = InlineParser.ParseInline(valueSource).FirstOrDefault() ?? new TextElement(valueSource);

                            yield return new WikiKeyValuePairElement(templateElement.SourceText, key, valueElement);
                            currentIndex = end;
                            continue;
                        }
                    }

                    yield return templateElement;
                    currentIndex = end;
                    continue;
                }
            }

            int endOfParagraph = FindEndOfParagraph(sourceText, currentIndex);
            string paragraphSource = sourceText.Substring(currentIndex, endOfParagraph - currentIndex).Trim();
            if (!string.IsNullOrEmpty(paragraphSource))
            {
                yield return new ParagraphElement(paragraphSource, InlineParser.ParseInline(paragraphSource).Select(x => x!));
            }
            currentIndex = endOfParagraph;
        }
    }

    private static int FindEndOfParagraph(string sourceText, int currentIndex)
    {
        int endOfParagraph = sourceText.Length;

        int[] nextBlockStarts =
        {
            sourceText.IndexOf("\n\n", currentIndex, StringComparison.Ordinal),
            sourceText.IndexOf("\n{|", currentIndex, StringComparison.Ordinal),
            sourceText.IndexOf("\n{{", currentIndex, StringComparison.Ordinal),
            sourceText.IndexOf("\n[[Category:", currentIndex, StringComparison.OrdinalIgnoreCase),
            sourceText.IndexOf("\n==", currentIndex, StringComparison.Ordinal)
        };

        foreach (var index in nextBlockStarts)
        {
            if (index != -1)
            {
                endOfParagraph = Math.Min(endOfParagraph, index);
            }
        }
        return endOfParagraph;
    }
}