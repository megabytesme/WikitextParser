namespace WikitextParser.Elements;


/// <summary>
/// Ref element, starts with xml open tag "ref" and ends with xml close tag "ref"
/// </summary>
public class RefElement : WikitextElement
{
    public WikitextElement ChildElement { get; }

    public RefElement(string sourceText, WikitextElement childElement) : base(WikitextElementType.Ref, sourceText)
    {
        ChildElement = childElement;
    }

    protected internal override string ToDebugString() => $"Ref: {ChildElement.ToDebugString()}";
}