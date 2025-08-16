namespace WikitextParser.Elements;

public class LinkElement : WikitextElement
{
    public string DisplayText { get; }
    public string Url { get; }

    public LinkElement(string sourceText, string displayText, string url) : base(WikitextElementType.Link, sourceText)
    {
        DisplayText = displayText;
        Url = url;
    }

    protected internal override string ToDebugString() => $"Link: {DisplayText}";
}