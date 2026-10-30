using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace WikitextParser.Elements
{
    /// <summary>A sequence of inline nodes without a paragraph wrapper.</summary>
    public class InlineContainerElement : WikitextElement
    {
        public IReadOnlyList<WikitextElement> ChildElements { get; }

        public InlineContainerElement(string sourceText, IEnumerable<WikitextElement> children)
            : base(WikitextElementType.InlineContainer, sourceText)
        {
            ChildElements = children.ToImmutableList();
        }

        public override string ConvertToHtml() => string.Concat(ChildElements.Select(c => c.ConvertToHtml()));
        public override string ConvertToText() => string.Concat(ChildElements.Select(c => c.ConvertToText()));
        protected internal override string ToDebugString() => "Inline: " + ConvertToText();
    }
}
