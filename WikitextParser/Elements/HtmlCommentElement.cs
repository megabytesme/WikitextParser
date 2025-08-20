namespace WikitextParser.Elements
{
    /// <summary>
    /// HTML comment element
    /// </summary>
    public class HtmlCommentElement : WikitextElement
    {
        public HtmlCommentElement(string sourceText) : base(WikitextElementType.Comment, sourceText)
        {
        }
    
        public override string ConvertToHtml() => SourceText;

        public override string ConvertToText() => "";

        protected internal override string ToDebugString() => $"HtmlComment: {SourceText}";
    }
}