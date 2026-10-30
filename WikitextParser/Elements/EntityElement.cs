using System.Net;

namespace WikitextParser.Elements
{
    public class EntityElement : WikitextElement
    {
        public EntityElement(string sourceText) : base(WikitextElementType.Entity, sourceText) { }
        public override string ConvertToHtml() => WebUtility.HtmlEncode(ConvertToText());
        public override string ConvertToText() => WebUtility.HtmlDecode(SourceText);
        protected internal override string ToDebugString() => SourceText;
    }
}
