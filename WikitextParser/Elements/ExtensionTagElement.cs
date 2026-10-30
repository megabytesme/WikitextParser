namespace WikitextParser.Elements
{
    /// <summary>An extension or unknown tag, retained verbatim for a host or server renderer.</summary>
    public class ExtensionTagElement : WikitextElement
    {
        public string TagName { get; }
        public string Attributes { get; }
        public string Content { get; }
        public ExtensionTagElement(string sourceText, string tagName, string attributes, string content)
            : base(WikitextElementType.ExtensionTag, sourceText) { TagName = tagName; Attributes = attributes; Content = content; }
        public override string ConvertToHtml() => System.Net.WebUtility.HtmlEncode(SourceText);
        public override string ConvertToText() => SourceText;
        protected internal override string ToDebugString() => "Extension: " + TagName;
    }
}
