using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace WikitextParser.Elements
{
    public class BlockContainerElement : WikitextElement
    {
        public IReadOnlyList<WikitextElement> ChildElements { get; }
        public BlockContainerElement(string sourceText, IEnumerable<WikitextElement> children)
            : base(WikitextElementType.BlockContainer, sourceText) { ChildElements = children.ToImmutableList(); }
        public override string ConvertToHtml() => string.Concat(ChildElements.Select(c => c.ConvertToHtml()));
        public override string ConvertToText() => string.Concat(ChildElements.Select(c => c.ConvertToText()));
        protected internal override string ToDebugString() => "Blocks: " + ChildElements.Count;
    }
}
