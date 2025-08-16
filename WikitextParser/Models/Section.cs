using System.Collections.Immutable;
using System.Text;
using WikitextParser.Elements;

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
    /// The direct content of this section, excluding subsections.
    /// </summary>
    public IReadOnlyList<WikitextElement> ContentElements { get; }

    /// <summary>
    /// A list of nested sections within this section.
    /// </summary>
    public IReadOnlyList<Section> Subsections { get; }

    internal Section(HeadingElement heading, IEnumerable<WikitextElement> contentElements, IEnumerable<Section> subsections)
    {
        Heading = heading;
        ContentElements = contentElements.ToImmutableList();
        Subsections = subsections.ToImmutableList();
    }

    /// <summary>
    /// Converts the entire section, including its subsections, to HTML.
    /// </summary>
    public string ConvertToHtml()
    {
        var sb = new StringBuilder();
        sb.Append(Heading.ConvertToHtml());
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

    /// <summary>
    /// Converts the entire section, including its subsections, to plain text.
    /// </summary>
    public string ConvertToText()
    {
        var sb = new StringBuilder();
        sb.Append(Heading.ConvertToText());
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