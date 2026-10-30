using System;
using System.Text.RegularExpressions;

namespace WikitextParser
{
    internal sealed class TagToken
    {
        internal string Name, Attributes;
        internal int ContentStart, ContentEnd, End;
        internal bool SelfClosing;
    }

    internal static class TagScanner
    {
        private static readonly Regex Tag = new Regex(@"<(?<close>/)?(?<name>[a-zA-Z][a-zA-Z0-9-]*)(?<attrs>(?:[^""'<>]|""[^""]*""|'[^']*')*)>");

        internal static bool TryRead(string text, int index, out TagToken token)
        {
            token = null;
            var opening = Tag.Match(text, index);
            if (!opening.Success || opening.Index != index || opening.Groups["close"].Success) return false;
            string name = opening.Groups["name"].Value.ToLowerInvariant();
            string attributes = opening.Groups["attrs"].Value.Trim();
            bool selfClosing = attributes.EndsWith("/", StringComparison.Ordinal) || name == "br" || name == "hr";
            int contentStart = opening.Index + opening.Length;
            token = new TagToken { Name = name, Attributes = attributes.TrimEnd('/').TrimEnd(),
                SelfClosing = selfClosing, ContentStart = contentStart, ContentEnd = contentStart, End = contentStart };
            if (selfClosing) return true;
            bool literal = name == "nowiki" || name == "pre" || name == "source" || name == "syntaxhighlight" || name == "math";
            int nesting = 1;
            for (var tag = Tag.Match(text, contentStart); tag.Success; tag = tag.NextMatch())
            {
                if (!string.Equals(tag.Groups["name"].Value, name, StringComparison.OrdinalIgnoreCase)) continue;
                if (tag.Groups["close"].Success) nesting--;
                else if (!literal && !tag.Groups["attrs"].Value.TrimEnd().EndsWith("/", StringComparison.Ordinal)) nesting++;
                if (nesting != 0) continue;
                token.ContentEnd = tag.Index;
                token.End = tag.Index + tag.Length;
                return true;
            }
            token = null;
            return false;
        }

        internal static bool TrySkipLiteral(string text, int index, out int end)
        {
            end = index;
            if (ParserUtils.StartsAt(text, index, "<!--"))
            {
                int close = text.IndexOf("-->", index + 4, StringComparison.Ordinal);
                end = close < 0 ? text.Length : close + 3;
                return true;
            }
            TagToken tag;
            if (text[index] == '<' && TryRead(text, index, out tag) &&
                (tag.Name == "nowiki" || tag.Name == "pre" || tag.Name == "source" || tag.Name == "syntaxhighlight" || tag.Name == "math"))
            { end = tag.End; return true; }
            return false;
        }
    }
}
