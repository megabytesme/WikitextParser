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
    private IEnumerable<TemplateParameterElement>? _parameters;

    public TemplateElement(string sourceText) : base(WikitextElementType.Template, sourceText)
    {
    }

    public required string TemplateName { get; init; }

    public IEnumerable<TemplateParameterElement> Parameters =>
        _parameters ??= TemplateParser.ParseTemplateParameters(this).ToImmutableList();

    public bool IsPlainlist => TemplateName == "Plainlist";

    public bool IsTransclusion => TemplateName.StartsWith(":");

    public bool IsInfobox => TemplateName.StartsWith("Infobox");

    public bool IsInfoboxOfType(string type) => IsInfobox && InfoboxType == type;

    public string? InfoboxType => _infoboxType ??= IsInfobox ? TemplateName.Split(' ', 2).Last() : null;

    public IEnumerable<WikitextElement> GetPlainListElements()
    {
        if (!IsPlainlist)
        {
            return [];
        }

        return _plainListElements ??= TemplateParser.ParsePlainList(this).Select(e => e!).ToImmutableList();
    }

    public override string ConvertToHtml()
    {
        if (IsPlainlist)
        {
            var items = GetPlainListElements()
                .Select(e => $"<li>{e.ConvertToHtml()}</li>");
            return $"<ul>{string.Concat(items)}</ul>";
        }
        return "";
    }

    public override string ConvertToText()
    {
        if (IsPlainlist)
        {
            return string.Concat(GetPlainListElements().Select(e => $"\n* {e.ConvertToText()}"));
        }
        return "";
    }

    protected internal override string ToDebugString()
    {
        StringBuilder sb = new();

        sb.Append($"Template: {TemplateName}");
        foreach (TemplateParameterElement param in Parameters)
        {
            sb.Append($"    {param.ToDebugString()}");
        }

        return sb.ToString();
    }
}