using System;
using System.Text;

namespace WikitextParser
{
    internal static class InclusionPreprocessor
    {
        internal static string Process(string source, InclusionMode mode)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (mode == InclusionMode.Preserve) return source;
            if (mode == InclusionMode.Transclusion)
            {
                var only = new StringBuilder(); bool found = false;
                for (int i = 0; i < source.Length; i++)
                {
                    int end;
                    if (TagScanner.TrySkipLiteral(source, i, out end)) { i = end - 1; continue; }
                    TagToken tag;
                    if (source[i] == '<' && TagScanner.TryRead(source, i, out tag) && tag.Name == "onlyinclude")
                    { found = true; only.Append(Controls(source.Substring(tag.ContentStart, tag.ContentEnd - tag.ContentStart), mode, 0)); i = tag.End - 1; }
                }
                if (found) return only.ToString();
            }
            return Controls(source, mode, 0);
        }
        private static string Controls(string source, InclusionMode mode, int depth)
        {
            if (depth >= 128) return source;
            var output = new StringBuilder();
            for (int i = 0; i < source.Length; i++)
            {
                int end;
                if (TagScanner.TrySkipLiteral(source, i, out end)) { output.Append(source, i, end - i); i = end - 1; continue; }
                TagToken tag;
                if (source[i] == '<' && TagScanner.TryRead(source, i, out tag) && (tag.Name == "noinclude" || tag.Name == "includeonly" || tag.Name == "onlyinclude"))
                {
                    bool hide = mode == InclusionMode.Page ? tag.Name == "includeonly" : tag.Name == "noinclude";
                    if (!hide) output.Append(Controls(source.Substring(tag.ContentStart, tag.ContentEnd - tag.ContentStart), mode, depth + 1));
                    i = tag.End - 1; continue;
                }
                output.Append(source[i]);
            }
            return output.ToString();
        }
    }
}
