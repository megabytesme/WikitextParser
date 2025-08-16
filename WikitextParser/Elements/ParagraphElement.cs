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