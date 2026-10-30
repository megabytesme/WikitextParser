namespace WikitextParser.Elements
{
    /// <summary>A supported HTML formatting tag; attributes remain available to custom renderers.</summary>
    public class HtmlElement : WikitextElement
    {
        public string TagName { get; }
        public string Attributes { get; }
        public WikitextElement Content { get; }
        public bool IsVoid { get; }

        public HtmlElement(string sourceText, string tagName, string attributes, WikitextElement content, bool isVoid)
            : base(WikitextElementType.Html, sourceText)
        {
            TagName = tagName;
            Attributes = attributes;
            Content = content;
            IsVoid = isVoid;
        }

        public override string ConvertToHtml() => "<" + TagName + HtmlAttributes.Render(Attributes) + ">" +
            (IsVoid ? "" : Content.ConvertToHtml() + "</" + TagName + ">");
        public override string ConvertToText() => TagName == "br" ? "\n" : Content.ConvertToText();
        protected internal override string ToDebugString() => "HTML: " + TagName;
    }
}
