namespace WikitextParser.Elements;

/// <summary>
/// Usually part of template with key value pairs as: image, image_alt, image2, image_alt2, ...
/// </summary>
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