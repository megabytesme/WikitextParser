namespace WikitextParser.Elements;

/// <summary>
/// Plain text
/// </summary>
public class TextElement : WikitextElement
{
    public TextElement(string sourceText) : base(WikitextElementType.Text, sourceText)
    {
    }

    protected internal override string ToDebugString() => SourceText;
}