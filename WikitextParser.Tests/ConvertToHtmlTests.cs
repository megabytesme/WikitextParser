using AwesomeAssertions;
using WikitextParser.Elements;

namespace WikitextParser.Tests;

public class ConvertToHtmlTests
{
    [Fact]
    public void BoldElement_ConvertsToHtml()
    {
        var element = new BoldElement("'''text'''", new TextElement("text"));
        element.ConvertToHtml().Should().Be("<strong>text</strong>");
    }

    [Fact]
    public void CategoryElement_ConvertsToHtml()
    {
        var element = new CategoryElement("[[Category:Test]]", "Test", null);
        element.ConvertToHtml().Should().BeEmpty();
    }

    [Fact]
    public void HeadingElement_ConvertsToHtml()
    {
        var element = new HeadingElement("==Header==", 2, new TextElement("Header"));
        element.ConvertToHtml().Should().Be("<h2>Header</h2>");
    }

    [Fact]
    public void HtmlCommentElement_ConvertsToHtml()
    {
        var element = new HtmlCommentElement("<!-- comment -->");
        element.ConvertToHtml().Should().Be("<!-- comment -->");
    }

    [Fact]
    public void ImageElement_ConvertsToHtml()
    {
        var element = new ImageElement("[[File:test.jpg|thumb|caption]]", "test.jpg", new[] { "thumb" }, "caption");
        var expected = "<img src=\"https://commons.wikimedia.org/wiki/File:test.jpg\" alt=\"caption\" title=\"caption\" />";
        element.ConvertToHtml().Should().Be(expected);
    }

    [Fact]
    public void ItalicElement_ConvertsToHtml()
    {
        var element = new ItalicElement("''text''", new TextElement("text"));
        element.ConvertToHtml().Should().Be("<em>text</em>");
    }

    [Fact]
    public void LinkElement_ConvertsToHtml()
    {
        var element = new LinkElement("[[Page Name|display]]", "display", "Page Name");
        element.ConvertToHtml().Should().Be("<a href=\"/wiki/Page_Name\">display</a>");
    }

    [Fact]
    public void ParagraphElement_ConvertsToHtml()
    {
        var children = new List<WikitextElement>
        {
            new TextElement("Some text with "),
            new ItalicElement("''italic''", new TextElement("italic"))
        };
        var element = new ParagraphElement("source", children);
        element.ConvertToHtml().Should().Be("<p>Some text with <em>italic</em></p>");
    }

    [Fact]
    public void RefElement_ConvertsToHtml()
    {
        var element = new RefElement("<ref>content</ref>", new TextElement("content"), null);
        // Note: The hash is deterministic for a given input, so this is a stable test.
        int refIndex = element.SourceText.GetHashCode();
        var expected = $"<sup><a href=\"#ref-{refIndex}\">[{Math.Abs(refIndex % 100)}]</a></sup>";
        element.ConvertToHtml().Should().Be(expected);
    }

    [Fact]
    public void TemplateElement_Plainlist_ConvertsToHtml()
    {
        // Arrange
        var wikitext = "{{Plainlist|\n* item 1\n* [[item 2]]\n}}";
        var template = new TemplateElement(wikitext) { TemplateName = "Plainlist" };

        // Act
        var html = template.ConvertToHtml();

        // Assert
        html.Should().Be("<ul><li>item 1</li><li><a href=\"/wiki/item_2\">item 2</a></li></ul>");
    }

    [Fact]
    public void TemplateElement_Other_ConvertsToHtml()
    {
        var element = new TemplateElement("{{Infobox}}") { TemplateName = "Infobox" };
        element.ConvertToHtml().Should().BeEmpty();
    }

    [Fact]
    public void TextElement_ConvertsToHtml()
    {
        var element = new TextElement("Text with < > & characters");
        element.ConvertToHtml().Should().Be("Text with &lt; &gt; &amp; characters");
    }

    [Fact]
    public void WikiKeyValuePairElement_ConvertsToHtml()
    {
        var element = new WikiKeyValuePairElement("{{key|val}}", "key", new TextElement("val"));
        element.ConvertToHtml().Should().BeEmpty();
    }

    [Fact]
    public void TableElement_ConvertsToHtml()
    {
        var cell = new TableCellElement("| cell", "", new TextElement("cell"), false);
        var row = new TableRowElement("|-", "", new[] { cell });
        var table = new TableElement("{|", "", new[] { row });
        table.ConvertToHtml().Should().Be("<table><tr><td>cell</td></tr></table>");
    }
}