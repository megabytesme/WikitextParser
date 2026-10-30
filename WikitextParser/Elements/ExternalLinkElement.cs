using System.Net;

namespace WikitextParser.Elements
{
    public class ExternalLinkElement : WikitextElement
    {
        public string Url { get; }
        public WikitextElement Label { get; }
        public bool IsBare { get; }

        public ExternalLinkElement(string sourceText, string url, WikitextElement label, bool isBare)
            : base(WikitextElementType.ExternalLink, sourceText)
        {
            Url = url;
            Label = label;
            IsBare = isBare;
        }

        public override string ConvertToHtml() => "<a href=\"" + WebUtility.HtmlEncode(Url) + "\">" + Label.ConvertToHtml() + "</a>";
        public override string ConvertToText() => Label.ConvertToText();
        protected internal override string ToDebugString() => "External link: " + Url;
    }
}
