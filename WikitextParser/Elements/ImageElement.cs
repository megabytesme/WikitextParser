using System.Collections.Immutable;

namespace WikitextParser.Elements;

/// <summary>
/// Image or File element, like [[File:MyImage.jpg|thumb|caption]]
/// </summary>
public class ImageElement : WikitextElement
{
    private string? _wikimediaLink;
    public string FileName { get; }
    public string? Caption { get; }
    public IReadOnlyList<string> Options { get; }

    public ImageElement(string sourceText, string fileName, IEnumerable<string> options, string? caption) : base(WikitextElementType.Image, sourceText)
    {
        FileName = fileName;
        Options = options.ToImmutableList();
        Caption = caption;
    }

    public string WikimediaLink => _wikimediaLink ??= $"https://commons.wikimedia.org/wiki/File:{FileName.Replace(" ", "_")}";

    protected internal override string ToDebugString()
    {
        var captionPart = Caption != null ? $" | {Caption}" : "";
        return $"Image: {FileName}{captionPart}";
    }
}