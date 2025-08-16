namespace WikitextParser.Elements;

public class BoldElement : WikitextElement
{
    public WikitextElement InnerElement { get; }

    public BoldElement(string sourceText, WikitextElement innerElement) : base(WikitextElementType.Bold, sourceText)
    {
        InnerElement = innerElement;
    }

    protected internal override string ToDebugString() => $"Bold: {InnerElement.ToDebugString()}";
}