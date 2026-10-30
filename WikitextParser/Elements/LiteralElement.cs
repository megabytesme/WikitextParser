using System.Net;

namespace WikitextParser.Elements
{
    /// <summary>Unparsed nowiki or pre content. SourceText retains the original tags.</summary>
    public class LiteralElement : WikitextElement
    {
        public string Content { get; }
        public bool IsPreformatted { get; }

        public LiteralElement(string sourceText, string content, bool isPreformatted = false)
            : base(WikitextElementType.Literal, sourceText)
        {
            Content = content;
            IsPreformatted = isPreformatted;
        }

        public override string ConvertToHtml()
        {
            var html = WebUtility.HtmlEncode(WebUtility.HtmlDecode(Content));
            return IsPreformatted ? "<pre>" + html + "</pre>" : html;
        }

        public override string ConvertToText() => WebUtility.HtmlDecode(Content);
        protected internal override string ToDebugString() => "Literal: " + Content;
    }
}
