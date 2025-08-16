using WikitextParser.Elements;
using System.Collections.Immutable;
using System.Text;

namespace WikitextParser.Models;

/// <summary>
/// Represents a section of a wiki page, defined by a heading and containing content and subsections.
/// </summary>
public class Section
{
    /// <summary>
    /// The heading element that defines this section.
    /// </summary>
    public HeadingElement Heading { get; }

    /// <summary>
    /// A list of templates, like {{Main|...}}, that appear at the start of the section.
    /// </summary>
    public IReadOnlyList<TemplateElement> MainArticleLinks { get; }

    /// <summary>
    /// The direct content of this section, excluding main article links and subsections.
    /// </summary>
    public IReadOnlyList<WikitextElement> ContentElements { get; }

    /// <summary>
    /// A list of nested sections within this section.
    /// </summary>
    public IReadOnlyList<Section> Subsections { get; }

    internal Section(HeadingElement heading, IEnumerable<TemplateElement> mainArticleLinks, IEnumerable<WikitextElement> contentElements, IEnumerable<Section> subsections)
    {
        Heading = heading;
        MainArticleLinks = mainArticleLinks.ToImmutableList();
        ContentElements = contentElements.ToImmutableList();
        Subsections = subsections.ToImmutableList();
    }

    public string ConvertToHtml()
    {
        var sb = new StringBuilder();
        sb.Append(Heading.ConvertToHtml());

        if (MainArticleLinks.Any())
        {
            sb.Append("<div class=\"main-article-links\">");
            foreach (var linkTemplate in MainArticleLinks)
            {
                var links = linkTemplate.Parameters.Select(p => p.Value.ConvertToHtml());
                sb.Append($"<p><i>{linkTemplate.TemplateName}: {string.Join(", ", links)}</i></p>");
            }
            sb.Append("</div>");
        }

        foreach (var element in ContentElements)
        {
            sb.Append(element.ConvertToHtml());
        }
        foreach (var subsection in Subsections)
        {
            sb.Append(subsection.ConvertToHtml());
        }
        return sb.ToString();
    }

    public string ConvertToText()
    {
        var sb = new StringBuilder();
        sb.Append(Heading.ConvertToText());

        if (MainArticleLinks.Any())
        {
            foreach (var linkTemplate in MainArticleLinks)
            {
                var links = linkTemplate.Parameters.Select(p => p.Value.ConvertToText());
                sb.Append($"{linkTemplate.TemplateName}: {string.Join(", ", links)}\n\n");
            }
        }

        foreach (var element in ContentElements)
        {
            sb.Append(element.ConvertToText());
        }
        foreach (var subsection in Subsections)
        {
            sb.Append(subsection.ConvertToText());
        }
        return sb.ToString();
    }
}