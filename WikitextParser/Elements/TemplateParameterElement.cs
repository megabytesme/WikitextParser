namespace WikitextParser.Elements
{
    /// <summary>
    /// Represents a single parameter inside a template.
    /// The parameter can be named (Key is not null) or positional (Key is null).
    /// </summary>
    public class TemplateParameterElement : WikitextElement
    {
        public string Key { get; }
        public WikitextElement Value { get; }

        /// <summary>
        /// Constructor for a template parameter.
        /// </summary>
        /// <param name="sourceText">The full source text of the parameter (e.g., "key=value" or just "value").</param>
        /// <param name="key">The name of the parameter. Null for positional parameters.</param>
        /// <param name="value">The parsed WikitextElement representing the parameter's value.</param>
        public TemplateParameterElement(string sourceText, string key, WikitextElement value) : base(WikitextElementType.TemplateParameter, sourceText)
        {
            Key = key;
            Value = value;
        }

        public override string ConvertToHtml() => Value.ConvertToHtml();

        public override string ConvertToText() => Value.ConvertToText();

        protected internal override string ToDebugString()
        {
            if (Key != null)
            {
                return $"Param: {Key} = {Value.ToDebugString()}";
            }
            return $"Param: {Value.ToDebugString()}";
        }
    }
}