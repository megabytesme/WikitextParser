using System.Collections.Generic;
using System.Linq;
using WikitextParser.Elements;

namespace WikitextParser
{
    /// <summary>Framework-independent access to nodes for native renderers and analysis.</summary>
    public static class ElementTraversal
    {
        public static IEnumerable<WikitextElement> Children(WikitextElement element)
        {
            if (element is ParagraphElement p) return p.ChildElements;
            if (element is InlineContainerElement i) return i.ChildElements;
            if (element is BlockContainerElement b) return b.ChildElements;
            if (element is BoldElement bold) return One(bold.InnerElement);
            if (element is ItalicElement italic) return One(italic.InnerElement);
            if (element is HeadingElement h) return One(h.ChildElement);
            if (element is HtmlElement html) return One(html.Content);
            if (element is RefElement r) return One(r.ChildElement);
            if (element is LinkElement link) return One(link.Label);
            if (element is ExternalLinkElement external) return One(external.Label);
            if (element is TemplateElement template) return template.Parameters;
            if (element is TemplateParameterElement parameter) return One(parameter.Value);
            if (element is TemplateArgumentElement argument) return One(argument.DefaultValue);
            if (element is PreformattedElement pre) return One(pre.Content);
            if (element is ListElement list) return list.Items;
            if (element is ListItemElement item) return One(item.Content).Concat(item.NestedLists);
            if (element is TableElement table) return One(table.Caption).Concat(table.Rows);
            if (element is TableRowElement row) return row.Cells;
            if (element is TableCellElement cell) return One(cell.Content);
            if (element is WikiKeyValuePairElement pair) return One(pair.Value);
            if (element is RedirectElement redirect) return One(redirect.Target);
            return new WikitextElement[0];
        }

        private static IEnumerable<WikitextElement> One(WikitextElement value)
        { if (value != null) yield return value; }

        public static IEnumerable<WikitextElement> DescendantsAndSelf(IEnumerable<WikitextElement> elements)
        {
            var stack = new Stack<WikitextElement>(elements.Reverse());
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                yield return current;
                foreach (var child in Children(current).Reverse()) stack.Push(child);
            }
        }
    }
}
