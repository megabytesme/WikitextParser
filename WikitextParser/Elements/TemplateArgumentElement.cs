namespace WikitextParser.Elements
{
    /// <summary>An unexpanded {{{argument|default}}} reference, distinct from invocation parameters.</summary>
    public class TemplateArgumentElement : WikitextElement
    {
        public string Name { get; }
        public WikitextElement DefaultValue { get; }
        public TemplateArgumentElement(string sourceText, string name, WikitextElement defaultValue)
            : base(WikitextElementType.TemplateArgument, sourceText) { Name = name; DefaultValue = defaultValue; }
        public override string ConvertToHtml() => System.Net.WebUtility.HtmlEncode(SourceText);
        public override string ConvertToText() => SourceText;
        protected internal override string ToDebugString() => "Template argument: " + Name;
    }
}
