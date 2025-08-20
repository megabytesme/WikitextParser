using WikitextParser.Elements;
using WikitextParser.Models;
using System.Collections.Generic; // Added
using System.Linq; // Added

namespace WikitextParser
{
    public static class Parser
    {
        private static readonly HashSet<string> _mainArticleTemplates = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            "Main",
            "See also"
        };


        /// <summary>
        /// Low-level API that parses a wikitext string into a flat list of elements.
        /// For a more structured output, consider using the higher-level <see cref="ParsePage"/> method.
        /// </summary>
        /// <param name="sourceText">The raw wikitext source string to parse.</param>
        /// <returns>An enumerable collection of the top-level <see cref="WikitextElement"/> objects found in the source text.</returns>
        public static IEnumerable<WikitextElement> Parse(string sourceText)
        {
            sourceText = sourceText.Replace("\r\n", "\n").Replace("\r", "\n");
            int currentIndex = 0;

            while (currentIndex < sourceText.Length)
            {
                while (currentIndex < sourceText.Length && char.IsWhiteSpace(sourceText[currentIndex]))
                {
                    currentIndex++;
                }
                if (currentIndex >= sourceText.Length) break;

                TableElement table;
                if (BlockParser.TryParseTable(sourceText, ref currentIndex, out table))
                {
                    yield return table;
                    continue;
                }
                HeadingElement heading;
                if (BlockParser.TryParseHeading(sourceText, ref currentIndex, out heading))
                {
                    yield return heading;
                    continue;
                }
                WikitextElement category;
                int nextIndex;
                if (BlockParser.TryParseCategoryLink(sourceText, ref currentIndex, out category, out nextIndex))
                {
                    yield return category;
                    currentIndex = nextIndex;
                    continue;
                }

                if (sourceText.Substring(currentIndex).StartsWith("{{"))
                {
                    int tempIndex = currentIndex;
                    WikitextElement templateElement;
                    int end;
                    if (BlockParser.TryParseTemplate(sourceText, ref tempIndex, out templateElement, out end))
                    {
                        string innerText = templateElement.SourceText.Substring(2, templateElement.SourceText.Length - 4).Trim();
                        if (!innerText.Contains("\n"))
                        {
                            var parts = ParserUtils.SplitAtTopLevel(innerText, '|').ToList();
                            string potentialKey = parts.FirstOrDefault()?.Trim() ?? "";

                            if (parts.Count == 2
                                && !(potentialKey.StartsWith("Infobox", System.StringComparison.OrdinalIgnoreCase))
                                && !_mainArticleTemplates.Contains(potentialKey))
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
                    yield return new ParagraphElement(paragraphSource, InlineParser.ParseInline(paragraphSource).Select(x => x));
                }
                currentIndex = endOfParagraph;
            }
        }

        /// <summary>
        /// High-level API that parses a wikitext string into a structured <see cref="Page"/> object.
        /// This method organizes the content into a lead section, an infobox, and a hierarchy of sections and subsections.
        /// </summary>
        /// <param name="sourceText">The wikitext of the entire page.</param>
        /// <returns>A <see cref="Page"/> object representing the structured document.</returns>
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
                HeadingElement heading = elements[i] as HeadingElement;
                if (heading == null || heading.HeadingLevel != level)
                {
                    i++;
                    continue;
                }

                int endOfSectionBlock = elements.FindIndex(i + 1, e => e is HeadingElement h && h.HeadingLevel <= level);
                if (endOfSectionBlock == -1)
                {
                    endOfSectionBlock = elements.Count;
                }

                var sectionBlock = elements.Skip(i + 1).Take(endOfSectionBlock - (i + 1)).ToList();
                int firstSubheadingIndex = sectionBlock.FindIndex(e => e is HeadingElement h && h.HeadingLevel > level);

                List<WikitextElement> contentBeforeSubsections;
                List<WikitextElement> subsectionElements;

                if (firstSubheadingIndex == -1)
                {
                    contentBeforeSubsections = sectionBlock;
                    subsectionElements = new List<WikitextElement>();
                }
                else
                {
                    contentBeforeSubsections = sectionBlock.Take(firstSubheadingIndex).ToList();
                    subsectionElements = sectionBlock.Skip(firstSubheadingIndex).ToList();
                }

                var mainArticleLinks = contentBeforeSubsections
                    .OfType<TemplateElement>()
                    .Where(t => _mainArticleTemplates.Contains(t.TemplateName))
                    .ToList();

                var directContent = contentBeforeSubsections
                    .Where(e => !(e is TemplateElement te && mainArticleLinks.Contains(te)))
                    .ToList();

                var subsections = BuildSectionHierarchy(subsectionElements, level + 1);

                sections.Add(new Section(heading, mainArticleLinks, directContent, subsections));
                i = endOfSectionBlock;
            }
            return sections;
        }

        private static int FindEndOfParagraph(string sourceText, int currentIndex)
        {
            int endOfParagraph = sourceText.Length;
            int[] nextBlockStarts =
            {
                sourceText.IndexOf("\n\n", currentIndex, System.StringComparison.Ordinal),
                sourceText.IndexOf("\n{|", currentIndex, System.StringComparison.Ordinal),
                sourceText.IndexOf("\n{{", currentIndex, System.StringComparison.Ordinal),
                sourceText.IndexOf("\n[[Category:", currentIndex, System.StringComparison.OrdinalIgnoreCase),
                sourceText.IndexOf("\n==", currentIndex, System.StringComparison.Ordinal)
            };
            foreach (var index in nextBlockStarts)
            {
                if (index != -1)
                {
                    endOfParagraph = System.Math.Min(endOfParagraph, index);
                }
            }
            return endOfParagraph;
        }
    }
}