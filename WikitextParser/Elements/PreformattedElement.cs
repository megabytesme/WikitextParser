namespace WikitextParser.Elements
{
    /// <summary>Space-prefixed preformatted lines, which still allow inline wikitext.</summary>
    public class PreformattedElement : WikitextElement
    {
        public WikitextElement Content { get; }
        public PreformattedElement(string sourceText, WikitextElement content)
            : base(WikitextElementType.Preformatted, sourceText) { Content = content; }
        public override string ConvertToHtml() => "<pre>" + Content.ConvertToHtml() + "</pre>";
        public override string ConvertToText() => Content.ConvertToText() + "\n";
        protected internal override string ToDebugString() => "Preformatted";
    }
}
