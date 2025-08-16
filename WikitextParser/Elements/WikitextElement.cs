using System.Diagnostics;

namespace WikitextParser.Elements;

[DebuggerDisplay("{ToDebugString()}")]
public abstract class WikitextElement
{
    protected WikitextElement(WikitextElementType type, string sourceText)
    {
        Type = type;
        SourceText = sourceText;
    }

    public WikitextElementType Type { get; }
    public string SourceText { get; }

    protected internal abstract string ToDebugString();
}