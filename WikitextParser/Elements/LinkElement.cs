using System.Net;

namespace WikitextParser.Elements
{
    /// <summary>
    /// Link element, in format [[Target]] or [[Target|DisplayText]].
    /// </summary>
    public class LinkElement : WikitextElement
    {
        public string DisplayText { get; }
        public string Url { get; }
        public WikitextElement Label { get; }

        public LinkElement(string sourceText, string displayText, string url) : base(WikitextElementType.Link, sourceText)
        {
            DisplayText = displayText;
            Url = url;
            Label = new TextElement(displayText);
        }

        public LinkElement(string sourceText, string displayText, string url, WikitextElement label)
            : this(sourceText, displayText, url) { Label = label; }

        public override string ConvertToHtml()
        {
            int hash = Url.IndexOf('#');
            string title = hash < 0 ? Url : Url.Substring(0, hash);
            string fragment = hash < 0 ? "" : "#" + System.Uri.EscapeDataString(Url.Substring(hash + 1).Replace(" ", "_"));
            string path = title.Length == 0 ? "" : "/wiki/" + System.Uri.EscapeDataString(title.Replace(" ", "_")).Replace("%3A", ":").Replace("%2F", "/");
            return "<a href=\"" + WebUtility.HtmlEncode(path + fragment) + "\">" + Label.ConvertToHtml() + "</a>";
        }

        public override string ConvertToText() => Label.ConvertToText();

        protected internal override string ToDebugString() => $"Link: {DisplayText}";
    }
}
