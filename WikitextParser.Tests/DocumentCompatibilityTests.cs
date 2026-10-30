using WikitextParser.Elements;
using WikitextParser.Models;

namespace WikitextParser.Tests;

public class DocumentCompatibilityTests
{
    [Fact]
    public void ReportsNestedUnresolvedConstructsForNativeRendering()
    {
        var document = Parser.ParseDocument("== Heading ==\n* {{T|{{{a|fallback}}}}}\n__TOC__ <math>x^2</math> <ref>citation</ref> [[File:A.png]]");
        Assert.True(document.RequiresWikiContext);
        Assert.Contains(document.CompatibilityIssues, i => i.Kind == CompatibilityIssueKind.TemplateExpansion);
        Assert.Contains(document.CompatibilityIssues, i => i.Kind == CompatibilityIssueKind.TemplateArgument);
        Assert.Contains(document.CompatibilityIssues, i => i.Kind == CompatibilityIssueKind.ExtensionTag);
        Assert.Contains(document.CompatibilityIssues, i => i.Kind == CompatibilityIssueKind.BehaviorSwitch);
        Assert.Contains(document.CompatibilityIssues, i => i.Kind == CompatibilityIssueKind.ReferenceLayout);
        Assert.Contains(document.CompatibilityIssues, i => i.Kind == CompatibilityIssueKind.ImageResolution);
    }

    [Fact]
    public void SimpleDocumentHasNoKnownContextRequirements()
    {
        var document = Parser.ParseDocument("== Heading ==\n* before [[Page|label]] after\n* <nowiki>{{literal}}</nowiki>");
        Assert.False(document.RequiresWikiContext);
        Assert.Empty(document.CompatibilityIssues);
    }

    [Fact]
    public void UnknownExtensionsRetainTheirSourceAndDoNotParseTheirBodies()
    {
        const string source = "<syntaxhighlight lang='csharp'>{{x}} <tag> &amp;</syntaxhighlight>";
        var extension = Assert.Single(ElementTraversal.DescendantsAndSelf(Parser.Parse(source)).OfType<ExtensionTagElement>());
        Assert.Equal(source, extension.SourceText);
        Assert.Equal("syntaxhighlight", extension.TagName);
        Assert.Equal("{{x}} <tag> &amp;", extension.Content);
        Assert.DoesNotContain(ElementTraversal.DescendantsAndSelf(Parser.Parse(source)), e => e is TemplateElement);
    }

    [Fact]
    public void RedirectIsNotAnOrderedList()
    {
        var document = Parser.ParseDocument("#REDIRECT [[Target#Section]]");
        Assert.Equal("Target#Section", Assert.IsType<RedirectElement>(Assert.Single(document.Elements)).Target.Url);
        Assert.Equal(CompatibilityIssueKind.RedirectResolution, Assert.Single(document.CompatibilityIssues).Kind);
    }

    [Fact]
    public void PreservesInfoboxInDocumentOrder()
    {
        var document = Parser.ParseDocument("{{Infobox test|a=b}}\nLead");
        Assert.IsType<TemplateElement>(document.Elements[0]);
        Assert.IsType<ParagraphElement>(document.Elements[1]);
    }

    [Fact]
    public void LiteralContentDoesNotGenerateCompatibilityIssues()
    {
        Assert.Empty(Parser.ParseDocument("<nowiki>__TOC__ {{T}} <math>x</math></nowiki>").CompatibilityIssues);
    }
}
