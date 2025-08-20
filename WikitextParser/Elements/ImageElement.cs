using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Net; // Changed from System.Web

namespace WikitextParser.Elements
{
    /// <summary>
    /// Image or File element, like [[File:MyImage.jpg|thumb|caption]]
    /// </summary>
    public class ImageElement : WikitextElement
    {
        private string _wikimediaLink;
        public string FileName { get; }
        public string Caption { get; }
        public IReadOnlyList<string> Options { get; }

        public ImageElement(string sourceText, string fileName, IEnumerable<string> options, string caption) : base(WikitextElementType.Image, sourceText)
        {
            FileName = fileName;
            Options = options.ToImmutableList();
            Caption = caption;
        }

        public string WikimediaLink
        {
            get
            {
                if (_wikimediaLink == null)
                {
                    _wikimediaLink = $"https://commons.wikimedia.org/wiki/File:{FileName.Replace(" ", "_")}";
                }
                return _wikimediaLink;
            }
        }

        public override string ConvertToHtml()
        {
            var altText = Options.FirstOrDefault(o => o.StartsWith("alt="))?.Split(new char[] { '=' }, 2)[1] ?? Caption ?? FileName;
            var sb = new StringBuilder();
            sb.Append($"<img src=\"{WikimediaLink}\" alt=\"{WebUtility.HtmlEncode(altText)}\"");

            if (Caption != null)
            {
                sb.Append($" title=\"{WebUtility.HtmlEncode(Caption)}\"");
            }
            sb.Append(" />");
            return sb.ToString();
        }

        public override string ConvertToText() => Caption ?? "";

        protected internal override string ToDebugString()
        {
            var captionPart = Caption != null ? $" | {Caption}" : "";
            return $"Image: {FileName}{captionPart}";
        }
    }
}