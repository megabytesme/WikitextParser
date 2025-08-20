using System.Net;

namespace WikitextParser.Elements
{
    /// <summary>
    /// Plain text
    /// </summary>
    public class TextElement : WikitextElement
    {
        public TextElement(string sourceText) : base(WikitextElementType.Text, sourceText)
        {
        }

        public override string ConvertToHtml() => WebUtility.HtmlEncode(SourceText);

        public override string ConvertToText() => SourceText;

        protected internal override string ToDebugString() => SourceText;
    }
}