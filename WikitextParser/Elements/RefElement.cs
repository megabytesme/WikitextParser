namespace WikitextParser.Elements;

/// <summary>
/// Ref element, starts with xml open tag "ref" and ends with xml close tag "ref"
/// Can be named and/or self-closing.
/// </summary>
public class RefElement : WikitextElement
{
    public string? Name { get; }
    public WikitextElement? ChildElement { get; }

    public RefElement(string sourceText, WikitextElement? childElement, string? name) : base(WikitextElementType.Ref, sourceText)
    {
        ChildElement = childElement;
        Name = name;
    }

    protected internal override string ToDebugString()
    {
        var namePart = Name != null ? $" Name: {Name}" : "";
        var childPart = ChildElement != null ? $" | {ChildElement.ToDebugString()}" : "";
        return $"Ref:{namePart}{childPart}";
    }
}