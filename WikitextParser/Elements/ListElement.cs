using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace WikitextParser.Elements
{
    public class ListElement : WikitextElement
    {
        public char Marker { get; }
        public IReadOnlyList<ListItemElement> Items { get; }
        public ListElement(string sourceText, char marker, IEnumerable<ListItemElement> items)
            : base(WikitextElementType.List, sourceText) { Marker = marker; Items = items.ToImmutableList(); }
        public string HtmlTag => Marker == '*' ? "ul" : Marker == '#' ? "ol" : "dl";
        public override string ConvertToHtml() => "<" + HtmlTag + ">" + string.Concat(Items.Select(i => i.ConvertToHtml())) + "</" + HtmlTag + ">";
        public override string ConvertToText() => string.Concat(Items.Select(i => i.ConvertToText()));
        protected internal override string ToDebugString() => "List: " + Marker;
    }

    public class ListItemElement : WikitextElement
    {
        public string Prefix { get; }
        public WikitextElement Content { get; }
        public IReadOnlyList<ListElement> NestedLists { get; }
        public ListItemElement(string sourceText, string prefix, WikitextElement content, IEnumerable<ListElement> nestedLists)
            : base(WikitextElementType.ListItem, sourceText)
        { Prefix = prefix; Content = content; NestedLists = nestedLists.ToImmutableList(); }
        public string HtmlTag => Prefix.EndsWith(";") ? "dt" : Prefix.EndsWith(":") ? "dd" : "li";
        public override string ConvertToHtml() => "<" + HtmlTag + ">" + Content.ConvertToHtml() +
            string.Concat(NestedLists.Select(l => l.ConvertToHtml())) + "</" + HtmlTag + ">";
        public override string ConvertToText() => Prefix + " " + Content.ConvertToText() + "\n" + string.Concat(NestedLists.Select(l => l.ConvertToText()));
        protected internal override string ToDebugString() => "List item: " + Prefix;
    }
}
