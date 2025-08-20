namespace WikitextParser.Elements
{
    /// <summary>
    /// Bold element, starts with two single quotes, ''.
    /// In cases when bold and italic are in single element five single quotes are used and bold is child of italic element.
    /// </summary>
    public class ItalicElement : WikitextElement
    {
        public WikitextElement InnerElement { get; }

        public ItalicElement(string sourceText, WikitextElement innerElement) : base(WikitextElementType.Italic, sourceText)
        {
            InnerElement = innerElement;
        }
    
        public override string ConvertToHtml() => $"<em>{InnerElement.ConvertToHtml()}</em>";

        public override string ConvertToText() => InnerElement.ConvertToText();

        protected internal override string ToDebugString() => $"Italic: {InnerElement.ToDebugString()}";
    }
}