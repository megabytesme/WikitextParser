using WikitextParser.Elements;

namespace WikitextParser.Tests;

public class InlineSyntaxTests
{
    private static string Html(string source) => string.Concat(Parser.Parse(source).Select(e => e.ConvertToHtml()));

    [Theory]
    [InlineData("'''before [[Target|label]] after'''", "<p><strong>before <a href=\"/wiki/Target\">label</a> after</strong></p>")]
    [InlineData("''before <sup>2</sup> after''", "<p><em>before <sup>2</sup> after</em></p>")]
    [InlineData("[[Help:Editing#A section|''label'']]", "<p><a href=\"/wiki/Help:Editing#A_section\"><em>label</em></a></p>")]
    [InlineData("[[#A section]]", "<p><a href=\"#A_section\">#A section</a></p>")]
    [InlineData("[[Help:Mercury (planet)|]]s", "<p><a href=\"/wiki/Help:Mercury_%28planet%29\">Mercurys</a></p>")]
    [InlineData("[[:Category:Science|science]]", "<p><a href=\"/wiki/Category:Science\">science</a></p>")]
    [InlineData("<nowiki>'''x''' [[y]] &amp;</nowiki>", "<p>&#39;&#39;&#39;x&#39;&#39;&#39; [[y]] &amp;</p>")]
    [InlineData("a<nowiki/>b", "<p>ab</p>")]
    [InlineData("&amp; &#65; &#x42; &unknown;", "<p>&amp; A B &amp;unknown;</p>")]
    [InlineData("[https://example.org/?a=1&b=2 ''site'']", "<p><a href=\"https://example.org/?a=1&amp;b=2\"><em>site</em></a></p>")]
    [InlineData("See https://example.org/a_(b).", "<p>See <a href=\"https://example.org/a_(b)\">https://example.org/a_(b)</a>.</p>")]
    [InlineData("[javascript:alert(1) bad]", "<p>[javascript:alert(1) bad]</p>")]
    [InlineData("<span title='a > b' onclick='bad()'>a<br/>b</span>", "<p><span title=\"a &gt; b\">a<br>b</span></p>")]
    [InlineData("<script>alert(1)</script>", "<p>&lt;script&gt;alert(1)&lt;/script&gt;</p>")]
    public void ConvertsInlineSyntax(string source, string expected) => Assert.Equal(expected, Html(source));

    [Fact]
    public void ReferencesAcceptCaseWhitespaceAndUnquotedNames()
    {
        var paragraph = Assert.IsType<ParagraphElement>(Assert.Single(Parser.Parse("a<REF name = test>one ''two'' three</REF>b<ref name='test'/>")));
        var refs = paragraph.ChildElements.OfType<RefElement>().ToList();
        Assert.Equal(2, refs.Count);
        Assert.Equal("test", refs[0].Name);
        Assert.Equal("one two three", refs[0].ChildElement.ConvertToText());
        Assert.Null(refs[1].ChildElement);
    }

    [Fact]
    public void MalformedReferenceDoesNotThrow() => Assert.Contains("name=", Html("a<ref name=>b"));

    [Fact]
    public void InlineSourceIsRetained()
    {
        const string source = "one '''two [[Target|three]] four''' &amp; <nowiki>x</nowiki>";
        var paragraph = Assert.IsType<ParagraphElement>(Assert.Single(Parser.Parse(source)));
        Assert.Equal(source, string.Concat(paragraph.ChildElements.Select(c => c.SourceText)));
        Assert.Equal("one two three four & x\n\n", paragraph.ConvertToText());
    }
}
