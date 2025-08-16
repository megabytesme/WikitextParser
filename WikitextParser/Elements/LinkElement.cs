using System.Web;

namespace WikitextParser.Elements;

/// <summary>
/// Link element, in format [[Url]] or [[DisplayText|Url]]
/// </summary>
public class LinkElement : WikitextElement
{
    public string DisplayText { get; }
    public string Url { get; }

    public LinkElement(string sourceText, string displayText, string url) : base(WikitextElementType.Link, sourceText)
    {
        DisplayText = displayText;
        Url = url;
    }
    public override string ConvertToHtml() => $"<a href=\"/wiki/{HttpUtility.UrlEncode(Url.Replace(" ", "_"))}\">{HttpUtility.HtmlEncode(DisplayText)}</a>";

    public override string ConvertToText() => DisplayText;

    protected internal override string ToDebugString() => $"Link: {DisplayText}";
}