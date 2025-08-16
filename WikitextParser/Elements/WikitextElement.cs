using System.Diagnostics;

namespace WikitextParser.Elements;

/// <summary>
/// Abstract base class for all element types
/// </summary>
[DebuggerDisplay("{ToDebugString()}")]
public abstract class WikitextElement
{
    protected WikitextElement(WikitextElementType type, string sourceText)
    {
        Type = type;
        SourceText = sourceText;
    }

    public WikitextElementType Type { get; }
    public string SourceText { get; }

    /// <summary>
    /// Converts the Wikitext element to its HTML representation.
    /// </summary>
    /// <returns>A string containing the HTML representation of the element.</returns>
    public abstract string ConvertToHtml();

    /// <summary>
    /// Converts the Wikitext element to its plain text representation.
    /// </summary>
    /// <returns>A string containing the plain text representation of the element.</returns>
    public abstract string ConvertToText();

    protected internal abstract string ToDebugString();
}