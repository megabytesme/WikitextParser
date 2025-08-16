namespace WikitextParser.Elements;

/// <summary>
/// Represents a single cell in a table row, starting with ! (header) or | (standard).
/// </summary>
public class TableCellElement : WikitextElement
{
    public string Attributes { get; }
    public WikitextElement Content { get; }
    public bool IsHeader { get; }

    public TableCellElement(string sourceText, string attributes, WikitextElement content, bool isHeader) : base(WikitextElementType.TableCell, sourceText)
    {
        Attributes = attributes;
        Content = content;
        IsHeader = isHeader;
    }

    public override string ConvertToHtml()
    {
        var tag = IsHeader ? "th" : "td";
        return $"<{tag} {(string.IsNullOrEmpty(Attributes) ? "" : " " + Attributes)}>{Content.ConvertToHtml()}</{tag}>";
    }

    public override string ConvertToText() => Content.ConvertToText();

    protected internal override string ToDebugString() => $"TableCell ({(IsHeader ? "Header" : "Data")}): {Content.ToDebugString()}";
}