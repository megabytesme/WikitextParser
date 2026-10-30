using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using WikitextParser.Elements;

namespace WikitextParser
{
    internal static class InlineParser
    {
        private static readonly HashSet<string> HtmlTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "b", "strong", "i", "em", "u", "s", "strike", "del", "ins", "sup", "sub", "small", "big", "code", "kbd", "samp", "var", "tt", "span", "div", "p", "blockquote", "center", "br", "hr", "ruby", "rb", "rt", "rp", "abbr", "cite", "q" };
        private static readonly Regex Entity = new Regex(@"\G&(?:#[0-9]+|#x[0-9a-fA-F]+|[a-zA-Z][a-zA-Z0-9]+);");
        private static readonly Regex Protocol = new Regex(@"\G(?:https?://|ftp://|mailto:|//)", RegexOptions.IgnoreCase);
        private static readonly Regex BehaviorSwitch = new Regex(@"\G__[A-Z][A-Z0-9_]*?__");

        internal static IEnumerable<WikitextElement> ParseInline(string text, int depth = 0, ParseContext context = null)
        {
            context = context ?? ParseContext.Default;
            if (string.IsNullOrEmpty(text)) yield break;
            if (depth >= 128) { yield return new TextElement(text); yield break; }
            int last = 0;
            for (int i = 0; i < text.Length;)
            {
                WikitextElement element;
                int end;
                if (!TryParse(text, i, depth, out element, out end, context)) { i++; continue; }
                if (i > last) yield return new TextElement(text.Substring(last, i - last));
                yield return element;
                last = i = end;
            }
            if (last < text.Length) yield return new TextElement(text.Substring(last));
        }

        internal static WikitextElement Content(string text, int depth = 0, ParseContext context = null)
        {
            var children = ParseInline(text, depth, context).ToList();
            return children.Count == 1 ? children[0] : new InlineContainerElement(text, children);
        }

        private static bool TryParse(string text, int i, int depth, out WikitextElement element, out int end, ParseContext context)
        {
            element = null;
            end = i;
            if (ParserUtils.StartsAt(text, i, "<!--"))
            {
                int close = text.IndexOf("-->", i + 4, StringComparison.Ordinal);
                end = close < 0 ? text.Length : close + 3;
                element = new HtmlCommentElement(text.Substring(i, end - i));
                return true;
            }
            if (text[i] == '<')
            {
                TagToken tag;
                if (TagScanner.TryRead(text, i, out tag))
                {
                    string source = text.Substring(i, tag.End - i), body = text.Substring(tag.ContentStart, tag.ContentEnd - tag.ContentStart);
                    if (tag.Name == "nowiki" || tag.Name == "pre") element = new LiteralElement(source, body, tag.Name == "pre");
                    else if (tag.Name == "ref") element = new RefElement(source, tag.SelfClosing ? null : Content(body, depth + 1, context),
                        HtmlAttributes.Parse(tag.Attributes).FirstOrDefault(a => a.Key.Equals("name", StringComparison.OrdinalIgnoreCase)).Value);
                    else if (HtmlTags.Contains(tag.Name)) element = new HtmlElement(source, tag.Name, tag.Attributes, Content(body, depth + 1, context), tag.SelfClosing);
                    else element = new ExtensionTagElement(source, tag.Name, tag.Attributes, body);
                    end = tag.End; return true;
                }
            }
            if (text[i] == '&')
            {
                var entity = Entity.Match(text, i);
                if (entity.Success && WebUtility.HtmlDecode(entity.Value) != entity.Value)
                { end = i + entity.Length; element = new EntityElement(entity.Value); return true; }
            }
            if (text[i] == '_')
            {
                var behavior = BehaviorSwitch.Match(text, i);
                if (behavior.Success) { end = i + behavior.Length; element = new BehaviorSwitchElement(behavior.Value); return true; }
            }
            foreach (int length in new[] { 5, 3, 2 })
            {
                string quotes = new string('\'', length);
                if (!ParserUtils.StartsAt(text, i, quotes)) continue;
                int close = text.IndexOf(quotes, i + length, StringComparison.Ordinal), newline = text.IndexOf('\n', i + length);
                if (close < 0 || (newline >= 0 && newline < close)) continue;
                string body = text.Substring(i + length, close - i - length);
                var content = Content(body, depth + 1, context);
                end = close + length;
                string source = text.Substring(i, end - i);
                element = length == 2 ? (WikitextElement)new ItalicElement(source, content) : new BoldElement(source, content);
                if (length == 5) element = new ItalicElement(source, element);
                return true;
            }
            if (ParserUtils.StartsAt(text, i, "{{{"))
            {
                int close = ParserUtils.FindMatchingDelimiters(text, i, "{{{", "}}}");
                if (close > i)
                {
                    var parts = ParserUtils.SplitAtTopLevel(text.Substring(i + 3, close - i - 6), '|').ToList();
                    end = close;
                    element = new TemplateArgumentElement(text.Substring(i, end - i), parts[0].Trim(),
                        parts.Count > 1 ? Content(string.Join("|", parts.Skip(1)), depth + 1, context) : null);
                    return true;
                }
            }
            int temp = i;
            if (BlockParser.TryParseFile(text, ref temp, out element, out end, context)) return true;
            temp = i;
            if (BlockParser.TryParseCategoryLink(text, ref temp, out element, out end, context)) return true;
            if (ParserUtils.StartsAt(text, i, "[["))
            {
                int close = ParserUtils.FindMatchingDelimiters(text, i, "[[", "]]");
                if (close > 0)
                {
                    var parts = ParserUtils.SplitAtTopLevel(text.Substring(i + 2, close - i - 4), '|').ToList();
                    string target = parts[0].Trim().TrimStart(':'), label = parts.Count > 1 ? string.Join("|", parts.Skip(1)) : target;
                    if (parts.Count > 1 && label.Length == 0) label = PipeTrick(target);
                    end = close;
                    end = context.TrailEnd(text, end);
                    label += text.Substring(close, end - close);
                    element = new LinkElement(text.Substring(i, end - i), label, target, Content(label, depth + 1, context));
                    return true;
                }
            }
            bool bracketed = text[i] == '[';
            int urlStart = bracketed ? i + 1 : i;
            if (Protocol.IsMatch(text, urlStart) && (bracketed || i == 0 || !char.IsLetterOrDigit(text[i - 1])))
            {
                int urlEnd = urlStart;
                while (urlEnd < text.Length && !char.IsWhiteSpace(text[urlEnd]) && "<>[]\"".IndexOf(text[urlEnd]) < 0) urlEnd++;
                if (urlEnd > urlStart)
                {
                    string label;
                    if (bracketed)
                    {
                        int close = text.IndexOf(']', urlEnd);
                        if (close < 0) return false;
                        label = text.Substring(urlEnd, close - urlEnd).Trim();
                        end = close + 1;
                    }
                    else
                    {
                        while (urlEnd > urlStart && ".,;:!?".IndexOf(text[urlEnd - 1]) >= 0) urlEnd--;
                        while (urlEnd > urlStart && text[urlEnd - 1] == ')' && text.Substring(urlStart, urlEnd - urlStart).Count(c => c == ')') > text.Substring(urlStart, urlEnd - urlStart).Count(c => c == '(')) urlEnd--;
                        end = urlEnd;
                        label = "";
                    }
                    string url = text.Substring(urlStart, urlEnd - urlStart);
                    // URL labels must be plain nodes to avoid parsing bare URLs recursively.
                    element = new ExternalLinkElement(text.Substring(i, end - i), url,
                        label.Length == 0 ? new TextElement(url) : Content(label, depth + 1, context), !bracketed);
                    return true;
                }
            }
            temp = i;
            return BlockParser.TryParseTemplate(text, ref temp, out element, out end, context, depth);
        }

        private static string PipeTrick(string target)
        {
            int colon = target.IndexOf(':');
            string label = colon >= 0 ? target.Substring(colon + 1) : target;
            int suffix = label.LastIndexOf(" (", StringComparison.Ordinal);
            if (suffix >= 0 && label.EndsWith(")", StringComparison.Ordinal)) label = label.Substring(0, suffix);
            int comma = label.IndexOf(',');
            return comma >= 0 ? label.Substring(0, comma) : label;
        }
    }
}
