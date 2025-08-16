namespace WikitextParser.Elements;

/// <summary>
/// Key value pair, starts with {{Key|Value}} value doesn't have to be plain text, it can be e.g. template
/// </summary>
public class WikiKeyValuePairElement : WikitextElement
{
    public string Key { get; }
    public WikitextElement Value { get; }

    public WikiKeyValuePairElement(string sourceText, string key, WikitextElement value) : base(WikitextElementType.KeyValuePair, sourceText)
    {
        Key = key;
        Value = value;
    }
    
    public override string ConvertToHtml() => "";

    public override string ConvertToText() => "";

    protected internal override string ToDebugString() => $"KeyValue: {Key} : {Value.ToDebugString()}";
}