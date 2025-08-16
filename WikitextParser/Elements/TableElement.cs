using System.Collections.Immutable;
using System.Text;

namespace WikitextParser.Elements;

/// <summary>
/// Represents a wikitext table, starting with {| and ending with |}
/// </summary>
public class TableElement : WikitextElement
{
    public string Attributes { get; }
    public IReadOnlyList<TableRowElement> Rows { get; }

    public TableElement(string sourceText, string attributes, IEnumerable<TableRowElement> rows) : base(WikitextElementType.Table, sourceText)
    {
        Attributes = attributes;
        Rows = rows.ToImmutableList();
    }

    public override string ConvertToHtml()
    {
        var sb = new StringBuilder();
        sb.Append($"<table {(string.IsNullOrEmpty(Attributes) ? "" : " " + Attributes)}>");
        foreach (var row in Rows)
        {
            sb.Append(row.ConvertToHtml());
        }
        sb.Append("</table>");
        return sb.ToString();
    }

    public override string ConvertToText()
    {
        return string.Concat(Rows.Select(r => r.ConvertToText())) + "\n";
    }

    protected internal override string ToDebugString() => $"Table ({Rows.Count} rows)";
}