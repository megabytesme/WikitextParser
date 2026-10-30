using WikitextParser.Elements;

namespace WikitextParser.Tests;

public class BlockSyntaxTests
{
    private static string Html(string text) => string.Concat(Parser.Parse(text).Select(e => e.ConvertToHtml()));

    [Theory]
    [InlineData("* one\n* two", "<ul><li>one</li><li>two</li></ul>")]
    [InlineData("* one\n*# two\n*# three\n* four", "<ul><li>one<ol><li>two</li><li>three</li></ol></li><li>four</li></ul>")]
    [InlineData("** child", "<ul><li><ul><li>child</li></ul></li></ul>")]
    [InlineData("* one\n# two\n: three", "<ul><li>one</li></ul><ol><li>two</li></ol><dl><dd>three</dd></dl>")]
    [InlineData("; term : definition\n: more", "<dl><dt>term</dt><dd>definition</dd><dd>more</dd></dl>")]
    [InlineData(": one\n:: two", "<dl><dd>one<dl><dd>two</dd></dl></dd></dl>")]
    [InlineData(" one\n  two '''bold'''", "<pre>one\n two <strong>bold</strong></pre>")]
    [InlineData("<pre>[[literal]]\n\n==literal==</pre>", "<pre>[[literal]]\n\n==literal==</pre>")]
    [InlineData("before\n----\nafter", "<p>before</p><hr><p>after</p>")]
    [InlineData("== before ''middle'' after ==", "<h2>before <em>middle</em> after</h2>")]
    [InlineData("== not closed", "<p>== not closed</p>")]
    [InlineData("one\n \t\ntwo", "<p>one</p><p>two</p>")]
    [InlineData("before<nowiki>\n\n== literal ==\n* literal</nowiki>after", "<p>before\n\n== literal ==\n* literalafter</p>")]
    [InlineData("<blockquote>one\n\n* two</blockquote>", "<blockquote><p>one</p><ul><li>two</li></ul></blockquote>")]
    public void ConvertsBlockSyntax(string source, string expected) => Assert.Equal(expected, Html(source));

    [Fact]
    public void PageRetainsSkippedAndInitialHeadingLevels()
    {
        var page = Parser.ParsePage("=== First ===\none\n===== Deep =====\ntwo\n= Root =\nthree\n=== Child ===\nfour");
        Assert.Equal(2, page.Sections.Count);
        Assert.Equal(5, page.Sections[0].Subsections[0].Heading.HeadingLevel);
        Assert.Equal(1, page.Sections[1].Heading.HeadingLevel);
        Assert.Equal(3, page.Sections[1].Subsections[0].Heading.HeadingLevel);
        foreach (string word in new[] { "one", "two", "three", "four" }) Assert.Contains(word, page.ConvertToText());
    }

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)]
    public void SupportsAllHeadingLevels(int level)
    {
        string marks = new('=', level);
        var heading = Assert.IsType<HeadingElement>(Assert.Single(Parser.Parse(marks + " Heading " + marks)));
        Assert.Equal(level, heading.HeadingLevel);
    }

    [Fact]
    public void ListEndsAtBlankLineOrOrdinaryText()
    {
        var blocks = Parser.Parse("* one\n* two\nparagraph\n\n# three").ToList();
        Assert.Collection(blocks, b => Assert.IsType<ListElement>(b), b => Assert.IsType<ParagraphElement>(b), b => Assert.IsType<ListElement>(b));
    }
}
