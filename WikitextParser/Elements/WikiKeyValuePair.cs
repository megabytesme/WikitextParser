namespace WikitextParser.Elements;

public class WikiKeyValuePairElement : WikitextElement
{
    public string Key { get; }
    public WikitextElement Value { get; }

    public WikiKeyValuePairElement(string sourceText, string key, WikitextElement value) : base(WikitextElementType.KeyValuePair, sourceText)
    {
        Key = key;
        Value = value;
    }

    protected internal override string ToDebugString() => $"KeyValue: {Key} : {Value.ToDebugString()}";
}