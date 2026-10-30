using WikitextParser.Elements;

namespace WikitextParser.Tests;

public class WikiGrammarTests
{
    private static ParserOptions German() => new()
    {
        FileNamespaces = new[] { "Datei", "Bild" }, CategoryNamespaces = new[] { "Kategorie" },
        RedirectAliases = new[] { "#WEITERLEITUNG" }, LinkTrailPattern = "[a-zäöüß]+",
        ImageOptionAliases = new Dictionary<string, string> { ["miniatur"] = "thumb", ["rechts"] = "right" }
    };

    [Fact]
    public void LocalizedNamespacesOptionsAndLinkTrailsUsePerParseContext()
    {
        var nodes = ElementTraversal.DescendantsAndSelf(Parser.Parse("[[Datei:A.svg|miniatur|rechts|Caption]] [[Kategorie:Test|Sort]] [[:Datei:A.svg|Link]] [[Haus]]häuser", German())).ToList();
        var image = Assert.Single(nodes.OfType<ImageElement>());
        Assert.Equal("A.svg", image.FileName);
        Assert.Contains("thumb", image.Options);
        Assert.Contains("right", image.Options);
        Assert.Single(nodes.OfType<CategoryElement>());
        Assert.Contains(nodes.OfType<LinkElement>(), l => l.Url == "Datei:A.svg");
        Assert.Contains(nodes.OfType<LinkElement>(), l => l.ConvertToText() == "Haushäuser");
        Assert.Empty(ElementTraversal.DescendantsAndSelf(Parser.Parse("[[Datei:A.svg]]")).OfType<ImageElement>());
    }

    [Fact]
    public void ContextFlowsThroughListsTablesHeadingsHtmlAndLazyTemplateArguments()
    {
        string source = "== [[Datei:H.png]] ==\n* [[Datei:L.png]]\n{|\n| [[Datei:T.png]]\n|}\n<div>[[Datei:D.png]]</div>\n{{Test|[[Datei:P.png]]}}";
        var document = Parser.ParseDocument(source, German());
        Assert.Equal(5, ElementTraversal.DescendantsAndSelf(document.Elements).OfType<ImageElement>().Count());
    }

    [Fact]
    public void RedirectAliasesAreNotNumberedLists()
    {
        var redirect = Assert.IsType<RedirectElement>(Assert.Single(Parser.Parse("#WEITERLEITUNG [[Ziel]]", German())));
        Assert.Equal("Ziel", redirect.Target.Url);
    }

    [Fact]
    public void OptionsAreSnapshottedBeforeLazyEnumeration()
    {
        var aliases = new List<string> { "Datei" }; var options = German(); options.FileNamespaces = aliases;
        var pending = Parser.Parse("{{T|[[Datei:A.png]]}}", options);
        aliases.Clear(); options.LinkTrailPattern = "[0-9]+";
        Assert.Single(ElementTraversal.DescendantsAndSelf(pending).OfType<ImageElement>());
    }

    [Theory]
    [InlineData(InclusionMode.Page, "ABDXF")]
    [InlineData(InclusionMode.Transclusion, "D")]
    public void InclusionControlsKeepOriginalSourceAndSelectTheCorrectContent(InclusionMode mode, string expected)
    {
        const string source = "A<noinclude>B</noinclude><includeonly>C</includeonly><onlyinclude>D</onlyinclude>X<includeonly>E</includeonly>F";
        var document = Parser.ParseDocument(source, new ParserOptions { InclusionMode = mode });
        Assert.Equal(source, document.SourceText);
        Assert.Equal(expected, document.PreprocessedText);
    }

    [Fact]
    public void TransclusionWithoutOnlyIncludeHonoursBothControlTags()
    {
        var document = Parser.ParseDocument("A<noinclude>B</noinclude><includeonly>C</includeonly>D", new ParserOptions { InclusionMode = InclusionMode.Transclusion });
        Assert.Equal("ACD", document.PreprocessedText);
    }

    [Fact]
    public void LiteralAndCommentedControlTagsDoNotSelectTransclusionRegions()
    {
        const string source = "<nowiki><onlyinclude>Literal</onlyinclude></nowiki><!-- <onlyinclude>Comment</onlyinclude> -->Text";
        var document = Parser.ParseDocument(source, new ParserOptions { InclusionMode = InclusionMode.Transclusion });
        Assert.Equal(source, document.PreprocessedText);
        Assert.Contains("Literal", string.Concat(document.Elements.Select(e => e.ConvertToText())));
    }

    [Fact]
    public void MalformedControlTagsAreRetainedAndPreserveModeDoesNotTransform()
    {
        const string source = "A<includeonly>Unclosed";
        Assert.Equal(source, Parser.ParseDocument(source).PreprocessedText);
        const string balanced = "A<noinclude>B</noinclude>";
        Assert.Equal(balanced, Parser.ParseDocument(balanced, new ParserOptions { InclusionMode = InclusionMode.Preserve }).PreprocessedText);
    }

    [Fact]
    public void DeepLazyTemplateTraversalKeepsTheDepthBound()
    {
        string source = new string('{', 0) + string.Concat(Enumerable.Repeat("{{T|", 200)) + "value" + string.Concat(Enumerable.Repeat("}}", 200));
        Assert.InRange(ElementTraversal.DescendantsAndSelf(Parser.Parse(source)).Count(), 1, 400);
    }
}
