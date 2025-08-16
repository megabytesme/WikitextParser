namespace WikitextParser.Elements;

public class ItalicElement : WikitextElement
{
    public WikitextElement InnerElement { get; }

    public ItalicElement(string sourceText, WikitextElement innerElement) : base(WikitextElementType.Italic, sourceText)
    {
        InnerElement = innerElement;
    }

    protected internal override string ToDebugString() => $"Italic: {InnerElement.ToDebugString()}";
}