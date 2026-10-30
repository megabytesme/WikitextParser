namespace WikitextParser.Elements
{
    public class HorizontalRuleElement : WikitextElement
    {
        public HorizontalRuleElement(string sourceText) : base(WikitextElementType.HorizontalRule, sourceText) { }
        public override string ConvertToHtml() => "<hr>";
        public override string ConvertToText() => "\n----\n";
        protected internal override string ToDebugString() => "Horizontal rule";
    }
}
