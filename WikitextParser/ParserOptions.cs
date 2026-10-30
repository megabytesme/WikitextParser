using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WikitextParser
{
    public enum InclusionMode { Page, Transclusion, Preserve }

    /// <summary>Wiki grammar supplied by the host. LinkTrailPattern is a .NET regex fragment, not MediaWiki PCRE.</summary>
    public sealed class ParserOptions
    {
        public IEnumerable<string> FileNamespaces { get; set; } = new[] { "File", "Image" };
        public IEnumerable<string> CategoryNamespaces { get; set; } = new[] { "Category" };
        public IEnumerable<string> RedirectAliases { get; set; } = new[] { "#REDIRECT" };
        public IDictionary<string, string> ImageOptionAliases { get; set; } = new Dictionary<string, string>();
        public string LinkTrailPattern { get; set; } = "[a-z]+";
        public InclusionMode InclusionMode { get; set; } = InclusionMode.Page;
    }

    internal sealed class ParseContext
    {
        internal static readonly ParseContext Default = new ParseContext(null);
        internal readonly HashSet<string> FileNamespaces, CategoryNamespaces;
        internal readonly string[] RedirectAliases;
        internal readonly Dictionary<string, string> ImageOptions;
        internal readonly Regex Trail;
        internal readonly InclusionMode Inclusion;
        internal ParseContext(ParserOptions options)
        {
            options = options ?? new ParserOptions();
            FileNamespaces = Names(options.FileNamespaces, new[] { "File", "Image" });
            CategoryNamespaces = Names(options.CategoryNamespaces, new[] { "Category" });
            RedirectAliases = (options.RedirectAliases ?? new string[0]).Concat(new[] { "#REDIRECT" }).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            ImageOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in options.ImageOptionAliases ?? new Dictionary<string, string>()) ImageOptions[pair.Key] = pair.Value;
            if ((options.LinkTrailPattern ?? "").Length > 512) throw new ArgumentException("Link trails must be a bounded .NET regex fragment.");
            Trail = new Regex(@"\G(?:" + (options.LinkTrailPattern ?? "[a-z]+") + ")", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
            Inclusion = options.InclusionMode;
        }
        private static HashSet<string> Names(IEnumerable<string> names, IEnumerable<string> defaults) => new HashSet<string>((names ?? new string[0]).Concat(defaults).Select(s => (s ?? "").Replace('_', ' ').Trim()), StringComparer.OrdinalIgnoreCase);
        internal int NamespaceLength(string text, int index, HashSet<string> names)
        {
            if (!ParserUtils.StartsAt(text, index, "[[")) return 0;
            int colon = text.IndexOf(':', index + 2);
            if (colon < 0 || colon - index > 200) return 0;
            return names.Contains(text.Substring(index + 2, colon - index - 2).Replace('_', ' ').Trim()) ? colon - index - 1 : 0;
        }
        internal int RedirectLength(string line)
        {
            foreach (var alias in RedirectAliases)
                if (line.StartsWith(alias, StringComparison.OrdinalIgnoreCase) && (line.Length == alias.Length || char.IsWhiteSpace(line[alias.Length]) || line[alias.Length] == '[')) return alias.Length;
            return 0;
        }
        internal int TrailEnd(string text, int start)
        {
            try { var match = Trail.Match(text, start); return match.Success ? start + match.Length : start; }
            catch (RegexMatchTimeoutException) { return start; }
        }
    }
}
