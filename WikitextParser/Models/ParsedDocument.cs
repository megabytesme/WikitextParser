using System.Collections.Generic;
using System.Collections.Immutable;
using WikitextParser.Elements;

namespace WikitextParser.Models
{
    public enum CompatibilityIssueKind
    {
        TemplateExpansion, TemplateArgument, ExtensionTag, BehaviorSwitch, ReferenceLayout, ImageResolution, RedirectResolution
    }

    public sealed class CompatibilityIssue
    {
        public CompatibilityIssueKind Kind { get; }
        public WikitextElement Element { get; }
        public string Message { get; }
        internal CompatibilityIssue(CompatibilityIssueKind kind, WikitextElement element, string message)
        { Kind = kind; Element = element; Message = message; }
    }

    /// <summary>Ordered syntax tree and known reasons to request server or host rendering.</summary>
    public sealed class ParsedDocument
    {
        public IReadOnlyList<WikitextElement> Elements { get; }
        public IReadOnlyList<CompatibilityIssue> CompatibilityIssues { get; }
        /// <summary>Known unresolved constructs exist. False is not a guarantee of MediaWiki parity.</summary>
        public bool RequiresWikiContext => CompatibilityIssues.Count > 0;

        internal ParsedDocument(IEnumerable<WikitextElement> elements)
        {
            Elements = elements.ToImmutableList();
            var issues = new List<CompatibilityIssue>();
            foreach (var node in ElementTraversal.DescendantsAndSelf(Elements))
            {
                if (node is TemplateElement template && !template.IsPlainlist || node is WikiKeyValuePairElement)
                    issues.Add(new CompatibilityIssue(CompatibilityIssueKind.TemplateExpansion, node, "Expand templates, magic words and parser functions using the originating wiki."));
                else if (node is TemplateArgumentElement)
                    issues.Add(new CompatibilityIssue(CompatibilityIssueKind.TemplateArgument, node, "Resolve this argument in its template invocation frame."));
                else if (node is ExtensionTagElement)
                    issues.Add(new CompatibilityIssue(CompatibilityIssueKind.ExtensionTag, node, "Render this tag using its wiki extension or a host handler."));
                else if (node is BehaviorSwitchElement)
                    issues.Add(new CompatibilityIssue(CompatibilityIssueKind.BehaviorSwitch, node, "Apply this switch using page and wiki context."));
                else if (node is RefElement)
                    issues.Add(new CompatibilityIssue(CompatibilityIssueKind.ReferenceLayout, node, "Use a document-level citation renderer for numbering, groups and backlinks."));
                else if (node is ImageElement)
                    issues.Add(new CompatibilityIssue(CompatibilityIssueKind.ImageResolution, node, "Resolve the file and thumbnail on the originating wiki."));
                else if (node is RedirectElement)
                    issues.Add(new CompatibilityIssue(CompatibilityIssueKind.RedirectResolution, node, "Follow the redirect using the originating wiki."));
            }
            CompatibilityIssues = issues.ToImmutableList();
        }
    }
}
