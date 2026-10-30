using System.Collections.Generic;
using System.Linq;
using WikitextParser.Elements;

namespace WikitextParser
{
    internal static class TemplateParser
    {
        internal static IEnumerable<WikitextElement> ParsePlainList(TemplateElement template)
        {
            string inner = template.SourceText.Substring(2, template.SourceText.Length - 4);
            var parts = ParserUtils.SplitAtTopLevel(inner, '|').ToList();
            if (parts.Count < 2) yield break;
            foreach (string line in parts[1].Split('\n'))
            {
                string item = line.Trim();
                if (item.StartsWith("*")) yield return InlineParser.Content(item.Substring(1).Trim());
            }
        }

        internal static IEnumerable<TemplateParameterElement> ParseTemplateParameters(TemplateElement template)
        {
            string inner = template.SourceText.Substring(2, template.SourceText.Length - 4);
            foreach (string parameter in ParserUtils.SplitAtTopLevel(inner, '|').Skip(1))
            {
                int equals = ParserUtils.TopLevelSeparators(parameter, "=").DefaultIfEmpty(-1).First();
                string key = equals < 0 ? null : parameter.Substring(0, equals).Trim();
                // Whitespace is significant in unnamed parameters. Empty slots still consume a position.
                string value = equals < 0 ? parameter : parameter.Substring(equals + 1).Trim();
                var children = InlineParser.ParseInline(value).ToList();
                WikitextElement content = children.Count == 1 ? children[0] : children.Count == 0 ?
                    (WikitextElement)new TextElement(value) : new ParagraphElement(value, children);
                yield return new TemplateParameterElement(parameter, key, content);
            }
        }
    }
}
