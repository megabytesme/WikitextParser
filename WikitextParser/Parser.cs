using WikitextParser.Elements;
using WikitextParser.Models;

namespace WikitextParser;

public static class Parser
{
    /// <summary>
    /// Low level API that will return page elements, consider using higher level API for parsing pages <see cref="ParsePage"/>
    /// </summary>
    /// <param name="sourceText">Wikitext source string</param>
    /// <returns>Collection of <see cref="WikitextElement"/></returns>
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
                        string potentialKey = parts.FirstOrDefault()?.Trim() ?? "";

                        // **FIXED LOGIC**: Added a check to ensure it's not an Infobox.
                        if (parts.Count == 2 && !potentialKey.StartsWith("Infobox", StringComparison.OrdinalIgnoreCase))
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

    /// <summary>
    /// Parses wikitext into a high-level, structured Page object.
    /// </summary>
    /// <param name="sourceText">The wikitext of the entire page.</param>
    /// <returns>A Page object representing the structured document.</returns>
    public static Page ParsePage(string sourceText)
    {
        var allElements = Parse(sourceText).ToList();
        var infobox = allElements.OfType<TemplateElement>().FirstOrDefault(t => t.IsInfobox);
        if (infobox != null)
        {
            allElements.Remove(infobox);
        }
        int firstHeadingIndex = allElements.FindIndex(e => e.Type == WikitextElementType.Heading);
        var leadContent = firstHeadingIndex == -1 ? allElements : allElements.Take(firstHeadingIndex).ToList();
        var remainingElements = firstHeadingIndex == -1 ? new List<WikitextElement>() : allElements.Skip(firstHeadingIndex).ToList();
        var topLevelSections = BuildSectionHierarchy(remainingElements, 2);
        return new Page(infobox, leadContent, topLevelSections);
    }

    private static List<Section> BuildSectionHierarchy(List<WikitextElement> elements, int level)
    {
        var sections = new List<Section>();
        int i = 0;
        while (i < elements.Count)
        {
            if (elements[i] is not HeadingElement heading || heading.HeadingLevel != level)
            {
                i++;
                continue;
            }

            // Find where this section's content block ends. 
            // It ends at the next heading of the same or a higher level.
            int endOfSectionBlock = elements.FindIndex(i + 1, e => e is HeadingElement h && h.HeadingLevel <= level);
            if (endOfSectionBlock == -1)
            {
                endOfSectionBlock = elements.Count;
            }

            // This list contains all content and all potential subsections for the current heading.
            var sectionBlock = elements.Skip(i + 1).Take(endOfSectionBlock - (i + 1)).ToList();

            // Find the start of the first subsection inside this block.
            int firstSubheadingIndex = sectionBlock.FindIndex(e => e is HeadingElement h && h.HeadingLevel > level);

            List<WikitextElement> directContent;
            List<Section> subsections;

            if (firstSubheadingIndex == -1)
            {
                // No subsections found, so the entire block is this section's direct content.
                directContent = sectionBlock;
                subsections = new List<Section>();
            }
            else
            {
                // The direct content is everything before the first subsection starts.
                directContent = sectionBlock.Take(firstSubheadingIndex).ToList();
                // The elements for the next recursive call are the first subheading and everything after it.
                var subsectionElements = sectionBlock.Skip(firstSubheadingIndex).ToList();
                subsections = BuildSectionHierarchy(subsectionElements, level + 1);
            }

            sections.Add(new Section(heading, directContent, subsections));
            i = endOfSectionBlock;
        }

        return sections;
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

        foreach (int index in nextBlockStarts)
        {
            if (index != -1)
            {
                endOfParagraph = Math.Min(endOfParagraph, index);
            }
        }
        return endOfParagraph;
    }
}