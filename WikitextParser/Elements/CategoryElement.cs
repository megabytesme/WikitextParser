namespace WikitextParser.Elements;

/// <summary>
/// Category link element, in format [[Category:Name]] or [[Category:Name|SortKey]]
/// </summary>
public class CategoryElement : WikitextElement
{
    public string CategoryName { get; }
    public string? SortKey { get; }

    public CategoryElement(string sourceText, string categoryName, string? sortKey) : base(WikitextElementType.Category, sourceText)
    {
        CategoryName = categoryName;
        SortKey = sortKey;
    }

    protected internal override string ToDebugString() => $"Category: {CategoryName}" + (SortKey != null ? $" | {SortKey}" : "");
}