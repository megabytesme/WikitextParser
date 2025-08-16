namespace WikitextParser.Elements;

public class ImageElement : WikitextElement
{
    private string? _wikimediaLink;

    public ImageElement(string sourceText) : base(WikitextElementType.Image, sourceText)
    {
    }

    public required string Name { get; init; }
    public required string Alt { get; init; }
    public string WikimediaLink => _wikimediaLink ??= $"https://commons.wikimedia.org/wiki/File:{Name.Replace(" ", "_")}";

    protected internal override string ToDebugString() => $"Image: {Name} | {Alt}";
}