using System.Collections.Immutable;
using System.Text;

namespace WikitextParser.Elements;

/// <summary>
/// Template element starts with {{template template-name
/// </summary>
public class TemplateElement : WikitextElement
{
    private string? _infoboxType;
    private IEnumerable<WikitextElement>? _plainListElements;
    private IEnumerable<WikiKeyValuePairElement>? _childElements;

    public TemplateElement(string sourceText) : base(WikitextElementType.Template, sourceText)
    {
    }

    public required string TemplateName { get; init; }

    public IEnumerable<WikiKeyValuePairElement> ChildElements =>
        _childElements ??= Parser.ParseTemplateKeyValuePairs(this).ToImmutableList();

    public bool IsPlainlist => TemplateName.StartsWith("Plainlist|");

    public bool IsInfobox => TemplateName.StartsWith("Infobox");

    public bool IsInfoboxOfType(string type) => IsInfobox && InfoboxType == type;

    public string? InfoboxType => _infoboxType ??= IsInfobox ? TemplateName.Split(' ', 2).Last() : null;

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
        foreach (WikiKeyValuePairElement pair in ChildElements)
        {
            sb.Append($"    {pair.ToDebugString()}");
        }

        return sb.ToString();
    }
}