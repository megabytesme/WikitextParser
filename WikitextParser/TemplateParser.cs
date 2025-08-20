using WikitextParser.Elements;
using System.Collections.Generic;
using System.Linq;

namespace WikitextParser
{
    /// <summary>
    /// Handles parsing the contents of a TemplateElement.
    /// </summary>
    internal static class TemplateParser
    {

        internal static IEnumerable<WikitextElement> ParsePlainList(TemplateElement template)
        {
            string innerText = template.SourceText.Substring(2, template.SourceText.Length - 4);

            int nameEndIndex = innerText.IndexOf('|');
            if (nameEndIndex == -1) yield break;
            string listText = innerText.Substring(nameEndIndex + 1);

            string[] items = listText.Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

            foreach (string item in items)
            {
                string trimmedItem = item.Trim();
                if (trimmedItem.StartsWith("*"))
                {
                    string itemContent = trimmedItem.Substring(1).Trim();
                    foreach (WikitextElement element in InlineParser.ParseInline(itemContent))
                    {
                        yield return element;
                    }
                }
            }
        }

        internal static IEnumerable<TemplateParameterElement> ParseTemplateParameters(TemplateElement template)
        {
            string innerText = template.SourceText.Substring(2, template.SourceText.Length - 4);

            int firstPipe = innerText.IndexOf('|');
            if (firstPipe == -1) yield break; // No parameters

            string paramsText = innerText.Substring(firstPipe + 1);

            var parameterStrings = SplitAtTopLevel(paramsText, '|');

            foreach (string paramStr in parameterStrings)
            {
                string trimmedParam = paramStr.Trim();
                if (string.IsNullOrEmpty(trimmedParam)) continue;

                string key = null;
                string valueSource;

                int equalsIndex = trimmedParam.IndexOf('=');
                if (equalsIndex > 0)
                {
                    key = trimmedParam.Substring(0, equalsIndex).Trim();
                    valueSource = trimmedParam.Substring(equalsIndex + 1).Trim();
                }
                else
                {
                    valueSource = trimmedParam;
                }

                var parsedValueElements = InlineParser.ParseInline(valueSource).ToList();
                WikitextElement valueElement;

                if (parsedValueElements.Count == 1)
                {
                    valueElement = parsedValueElements.First();
                }
                else if (parsedValueElements.Any())
                {
                    valueElement = new ParagraphElement(valueSource, parsedValueElements.Select(e => e));
                }
                else
                {
                    valueElement = new TextElement(valueSource);
                }

                yield return new TemplateParameterElement(trimmedParam, key, valueElement);
            }
        }

        private static IEnumerable<string> SplitAtTopLevel(string text, char separator)
        {
            int level = 0;
            int lastSplit = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (i + 1 < text.Length)
                {
                    var sub = text.Substring(i, 2);
                    if (sub == "{{" || sub == "[[")
                    {
                        level++;
                        i++;
                        continue;
                    }
                    if (sub == "}}" || sub == "]]")
                    {
                        level--;
                        i++;
                        continue;
                    }
                }

                if (text[i] == separator && level == 0)
                {
                    yield return text.Substring(lastSplit, i - lastSplit);
                    lastSplit = i + 1;
                }
            }
            yield return text.Substring(lastSplit);
        }
    }
}