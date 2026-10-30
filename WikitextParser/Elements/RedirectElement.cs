namespace WikitextParser.Elements
{
    public class RedirectElement : WikitextElement
    {
        public LinkElement Target { get; }
        public RedirectElement(string sourceText, LinkElement target) : base(WikitextElementType.Redirect, sourceText) { Target = target; }
        public override string ConvertToHtml() => "<p class=\"redirect\">Redirect to " + Target.ConvertToHtml() + "</p>";
        public override string ConvertToText() => "Redirect to " + Target.ConvertToText();
        protected internal override string ToDebugString() => "Redirect: " + Target.Url;
    }
}
