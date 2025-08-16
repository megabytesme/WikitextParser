using System.Collections.Immutable;
using System.Text;

namespace WikitextParser.Elements;

/// <summary>
/// Paragraph, text separated by empty line that is not of another element type
/// </summary>
public class ParagraphElement : WikitextElement
{
    public ParagraphElement(string sourceText, IEnumerable<WikitextElement> childElements) : base(WikitextElementType.Paragraph, sourceText)
    {
        ChildElements = childElements.ToImmutableList();
    }

    public IEnumerable<WikitextElement> ChildElements { get; }
    
    public override string ConvertToHtml() => $"<p>{string.Concat(ChildElements.Select(c => c.ConvertToHtml()))}</p>";

    public override string ConvertToText() => $"{string.Concat(ChildElements.Select(c => c.ConvertToText()))}\n\n";

    protected internal override string ToDebugString()
    {
        StringBuilder sb = new();
        sb.Append("Paragraph: ");

        foreach (WikitextElement child in ChildElements)
        {
            {
                sb.Append(child.ToDebugString());
            }
        }

        return sb.ToString();
    }
}