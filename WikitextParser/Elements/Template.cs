using System.Collections.Immutable;
using System.Text;

namespace WikitextParser.Elements;

public class TemplateElement : WikitextElement
{
    private string? _infoboxType;
    private IEnumerable<WikitextElement>? _plainListElements;
    private IEnumerable<WikiKeyValuePairElement>? _templateElements;

    public TemplateElement(WikitextElementType type, string sourceText) : base(type, sourceText)
    {
    }

    public required string TemplateName { get; init; }

    public bool IsPlainlist => TemplateName.StartsWith("Plainlist|");

    public bool IsInfobox => TemplateName.StartsWith("Infobox");

    public bool IsInfoboxOfType(string type) => IsInfobox && InfoboxType == type;

    public string? InfoboxType => _infoboxType ??= IsInfobox ? TemplateName.Split(' ', 2).Last() : null;

    public IEnumerable<WikiKeyValuePairElement> GetTemplateElements()
    {
        return _templateElements ??= Parser.ParseTemplateKeyValuePairs(this).ToImmutableList();
    }

    public IEnumerable<WikitextElement> GetPlainListElements()
    {
        if (!IsPlainlist)
        {
            return [];
        }

        return _plainListElements ??= Parser.ParsePlainList(this).ToImmutableList();
    }

    protected internal override string ToDebugString()
    {
        StringBuilder sb = new();

        sb.Append($"Template: {TemplateName}");
        foreach (WikiKeyValuePairElement pair in GetTemplateElements())
        {
            sb.Append($"    {pair.ToDebugString()}");
        }

        return sb.ToString();
    }
}
