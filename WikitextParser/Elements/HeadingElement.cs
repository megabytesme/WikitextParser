namespace WikitextParser.Elements;

/// <summary>
/// Heading element, H2 starts with ==, H3 with ===, etc...
/// Sample of heading H3: ===Sub Title===
/// </summary>
public class HeadingElement : WikitextElement
{
    public int HeadingLevel { get; }
    public WikitextElement ChildElement { get; }

    public HeadingElement(string sourceText, int headingLevel, WikitextElement childElement) : base(WikitextElementType.Heading, sourceText)
    {
        HeadingLevel = headingLevel;
        ChildElement = childElement;
    }

    protected internal override string ToDebugString() => $"H{HeadingLevel}: {ChildElement.ToDebugString()}";
}