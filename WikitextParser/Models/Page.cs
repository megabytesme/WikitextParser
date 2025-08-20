using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using WikitextParser.Elements;

namespace WikitextParser.Models
{
    /// <summary>
    /// Represents a high-level, structured wiki page, with a lead section, an infobox, and a hierarchy of sections.
    /// </summary>
    public class Page
    {
        /// <summary>
        /// The main infobox of the page, if one exists.
        /// </summary>
        public TemplateElement Infobox { get; }

        /// <summary>
        /// The content that appears before the first heading.
        /// </summary>
        public IReadOnlyList<WikitextElement> LeadContent { get; }

        /// <summary>
        /// The top-level sections (H2) of the page.
        /// </summary>
        public IReadOnlyList<Section> Sections { get; }

        internal Page(TemplateElement infobox, IEnumerable<WikitextElement> leadContent, IEnumerable<Section> sections)
        {
            Infobox = infobox;
            LeadContent = leadContent.ToImmutableList();
            Sections = sections.ToImmutableList();
        }

        /// <summary>
        /// Converts the entire page to its HTML representation.
        /// </summary>
        public string ConvertToHtml()
        {
            var sb = new StringBuilder();
            foreach (var element in LeadContent)
            {
                sb.Append(element.ConvertToHtml());
            }
            foreach (var section in Sections)
            {
                sb.Append(section.ConvertToHtml());
            }
            return sb.ToString();
        }

        /// <summary>
        /// Converts the entire page to its plain text representation.
        /// </summary>
        public string ConvertToText()
        {
            var sb = new StringBuilder();
            foreach (var element in LeadContent)
            {
                sb.Append(element.ConvertToText());
            }
            foreach (var section in Sections)
            {
                sb.Append(section.ConvertToText());
            }
            return sb.ToString().Trim();
        }
    }
}