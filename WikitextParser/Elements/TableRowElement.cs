using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace WikitextParser.Elements
{
    /// <summary>
    /// Represents a row in a wikitext table, usually starting with |-
    /// </summary>
    public class TableRowElement : WikitextElement
    {
        public string Attributes { get; }
        public IReadOnlyList<TableCellElement> Cells { get; }

        public TableRowElement(string sourceText, string attributes, IEnumerable<TableCellElement> cells) : base(WikitextElementType.TableRow, sourceText)
        {
            Attributes = attributes;
            Cells = cells.ToImmutableList();
        }

        public override string ConvertToHtml()
        {
            var sb = new StringBuilder();
            var attributesPart = string.IsNullOrEmpty(Attributes) ? "" : " " + Attributes;
            sb.Append($"<tr{attributesPart}>");
            foreach (var cell in Cells)
            {
                sb.Append(cell.ConvertToHtml());
            }
            sb.Append("</tr>");
            return sb.ToString();
        }

        public override string ConvertToText()
        {
            return "| " + string.Join(" | ", Cells.Select(c => c.ConvertToText().Replace("\n", " ").Trim())) + " |\n";
        }

        protected internal override string ToDebugString() => $"TableRow ({Cells.Count} cells)";
    }
}