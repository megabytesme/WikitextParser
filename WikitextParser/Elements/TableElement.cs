using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace WikitextParser.Elements
{
    /// <summary>
    /// Represents a wikitext table, starting with {| and ending with |}
    /// </summary>
    public class TableElement : WikitextElement
    {
        public string Attributes { get; }
        public IReadOnlyList<TableRowElement> Rows { get; }
        public WikitextElement Caption { get; }
        public string CaptionAttributes { get; }

        public TableElement(string sourceText, string attributes, IEnumerable<TableRowElement> rows) : base(WikitextElementType.Table, sourceText)
        {
            Attributes = attributes;
            Rows = rows.ToImmutableList();
        }

        public TableElement(string sourceText, string attributes, IEnumerable<TableRowElement> rows, WikitextElement caption, string captionAttributes)
            : this(sourceText, attributes, rows) { Caption = caption; CaptionAttributes = captionAttributes; }

        public override string ConvertToHtml()
        {
            var sb = new StringBuilder();
            var attributesPart = HtmlAttributes.Render(Attributes);
            sb.Append($"<table{attributesPart}>");
            if (Caption != null) sb.Append("<caption" + HtmlAttributes.Render(CaptionAttributes) + ">" + Caption.ConvertToHtml() + "</caption>");
            foreach (var row in Rows)
            {
                sb.Append(row.ConvertToHtml());
            }
            sb.Append("</table>");
            return sb.ToString();
        }

        public override string ConvertToText()
        {
            return (Caption == null ? "" : Caption.ConvertToText() + "\n") + string.Concat(Rows.Select(r => r.ConvertToText())) + "\n";
        }

        protected internal override string ToDebugString() => $"Table ({Rows.Count} rows)";
    }
}
