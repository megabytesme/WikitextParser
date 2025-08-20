namespace WikitextParser.Elements
{
    /// <summary>
    /// Bold element, starts with three single quotes, '''.
    /// In cases when bold and italic are in single element five single quotes are used and bold is child of italic element.
    /// </summary>
    public class BoldElement : WikitextElement
    {
        public WikitextElement InnerElement { get; }

        public BoldElement(string sourceText, WikitextElement innerElement) : base(WikitextElementType.Bold, sourceText)
        {
            InnerElement = innerElement;
        }
    
        public override string ConvertToHtml() => $"<strong>{InnerElement.ConvertToHtml()}</strong>";

        public override string ConvertToText() => InnerElement.ConvertToText();

        protected internal override string ToDebugString() => $"Bold: {InnerElement.ToDebugString()}";
    }
}