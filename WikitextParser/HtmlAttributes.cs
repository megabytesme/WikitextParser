using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace WikitextParser
{
    internal static class HtmlAttributes
    {
        private static readonly HashSet<string> Allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "class", "id", "title", "lang", "dir", "colspan", "rowspan", "scope", "headers", "align", "valign", "width", "height"
        };
        private static readonly Regex Attribute = new Regex(@"([\w:-]+)\s*=\s*(?:""([^""]*)""|'([^']*)'|([^\s>]+))");

        internal static IEnumerable<KeyValuePair<string, string>> Parse(string source)
        {
            foreach (Match match in Attribute.Matches(source ?? ""))
                yield return new KeyValuePair<string, string>(match.Groups[1].Value,
                    WebUtility.HtmlDecode(match.Groups[2].Success ? match.Groups[2].Value :
                    match.Groups[3].Success ? match.Groups[3].Value : match.Groups[4].Value));
        }

        internal static string Render(string source)
        {
            var result = new StringBuilder();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var attribute in Parse(source))
                if (Allowed.Contains(attribute.Key) && seen.Add(attribute.Key))
                    result.Append(" ").Append(attribute.Key.ToLowerInvariant()).Append("=\"")
                        .Append(WebUtility.HtmlEncode(attribute.Value)).Append("\"");
            return result.ToString();
        }
    }
}
