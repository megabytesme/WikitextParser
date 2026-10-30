using System;
using System.Collections.Generic;
using System.Linq;
using WikitextParser.Elements;
using WikitextParser.Models;

namespace WikitextParser
{
    public static class Parser
    {
        private static readonly HashSet<string> MainArticleTemplates = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Main", "See also" };

        /// <summary>Parse block and inline syntax without expanding wiki-specific templates.</summary>
        public static IEnumerable<WikitextElement> Parse(string sourceText) => Parse(sourceText, null);
        public static IEnumerable<WikitextElement> Parse(string sourceText, ParserOptions options)
        {
            var context = new ParseContext(options);
            return DocumentParser.Parse(InclusionPreprocessor.Process(sourceText, context.Inclusion), 0, context);
        }

        /// <summary>Parse an ordered syntax tree with known compatibility gaps for host/server fallback.</summary>
        public static ParsedDocument ParseDocument(string sourceText) => ParseDocument(sourceText, null);
        public static ParsedDocument ParseDocument(string sourceText, ParserOptions options)
        {
            var context = new ParseContext(options);
            string prepared = InclusionPreprocessor.Process(sourceText, context.Inclusion);
            return new ParsedDocument(DocumentParser.Parse(prepared, 0, context), sourceText, prepared);
        }

        /// <summary>Organize parsed elements into a lead, infobox and sections at any heading level.</summary>
        public static Page ParsePage(string sourceText) => ParsePage(sourceText, null);
        public static Page ParsePage(string sourceText, ParserOptions options)
        {
            var elements = Parse(sourceText, options).ToList();
            var infobox = elements.OfType<TemplateElement>().FirstOrDefault(t => t.IsInfobox);
            if (infobox != null) elements.Remove(infobox);
            int index = elements.FindIndex(e => e is HeadingElement);
            if (index < 0) return new Page(infobox, elements, new Section[0]);
            var lead = elements.Take(index).ToList();
            var sections = new List<Section>();
            while (index < elements.Count) sections.Add(ReadSection(elements, ref index));
            return new Page(infobox, lead, sections);
        }

        private static Section ReadSection(List<WikitextElement> elements, ref int index)
        {
            var heading = (HeadingElement)elements[index++];
            var content = new List<WikitextElement>();
            while (index < elements.Count && !(elements[index] is HeadingElement)) content.Add(elements[index++]);
            var main = content.OfType<TemplateElement>().Where(t => MainArticleTemplates.Contains(t.TemplateName)).ToList();
            content.RemoveAll(e => e is TemplateElement t && main.Contains(t));
            var children = new List<Section>();
            while (index < elements.Count && ((HeadingElement)elements[index]).HeadingLevel > heading.HeadingLevel)
                children.Add(ReadSection(elements, ref index));
            return new Section(heading, main, content, children);
        }
    }
}
