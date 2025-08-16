using AwesomeAssertions;
using WikitextParser.Elements;

namespace WikitextParser.Tests;

public class ConvertToTextTests
{
    [Fact]
    public void BoldElement_ConvertsToText()
    {
        var element = new BoldElement("'''text'''", new TextElement("text"));
        element.ConvertToText().Should().Be("text");
    }

    [Fact]
    public void CategoryElement_ConvertsToText()
    {
        var element = new CategoryElement("[[Category:Test]]", "Test", null);
        element.ConvertToText().Should().BeEmpty();
    }

    [Fact]
    public void HeadingElement_ConvertsToText()
    {
        var element = new HeadingElement("==Header==", 2, new TextElement("Header"));
        element.ConvertToText().Should().Be("\n\nHeader\n\n");
    }

    [Fact]
    public void HtmlCommentElement_ConvertsToText()
    {
        var element = new HtmlCommentElement("<!-- comment -->");
        element.ConvertToText().Should().BeEmpty();
    }

    [Fact]
    public void ImageElement_ConvertsToText()
    {
        var element = new ImageElement("[[File:test.jpg|thumb|caption]]", "test.jpg", new[] { "thumb" }, "caption");
        element.ConvertToText().Should().Be("caption");

        var elementNoCaption = new ImageElement("[[File:test.jpg]]", "test.jpg", new string[] { }, null);
        elementNoCaption.ConvertToText().Should().BeEmpty();
    }

    [Fact]
    public void ItalicElement_ConvertsToText()
    {
        var element = new ItalicElement("''text''", new TextElement("text"));
        element.ConvertToText().Should().Be("text");
    }

    [Fact]
    public void LinkElement_ConvertsToText()
    {
        var element = new LinkElement("[[Page Name|display]]", "display", "Page Name");
        element.ConvertToText().Should().Be("display");
    }

    [Fact]
    public void ParagraphElement_ConvertsToText()
    {
        var children = new List<WikitextElement>
        {
            new TextElement("Some text with "),
            new ItalicElement("''italic''", new TextElement("italic"))
        };
        var element = new ParagraphElement("source", children);
        element.ConvertToText().Should().Be("Some text with italic\n\n");
    }

    [Fact]
    public void RefElement_ConvertsToText()
    {
        var element = new RefElement("<ref>content</ref>", new TextElement("content"), null);
        element.ConvertToText().Should().BeEmpty();
    }

    [Fact]
    public void TemplateElement_Plainlist_ConvertsToText()
    {
        // Arrange
        var wikitext = "{{Plainlist|\n* item 1\n* [[item 2]]\n}}";
        var template = new TemplateElement(wikitext) { TemplateName = "Plainlist|" };

        // Act
        var text = template.ConvertToText();

        // Assert
        text.Should().Be("\n* item 1\n* item 2");
    }

    [Fact]
    public void TemplateElement_Other_ConvertsToText()
    {
        var element = new TemplateElement("{{Infobox}}") { TemplateName = "Infobox" };
        element.ConvertToText().Should().BeEmpty();
    }

    [Fact]
    public void TextElement_ConvertsToText()
    {
        var element = new TextElement("Just plain text");
        element.ConvertToText().Should().Be("Just plain text");
    }

    [Fact]
    public void WikiKeyValuePairElement_ConvertsToText()
    {
        var element = new WikiKeyValuePairElement("{{key|val}}", "key", new TextElement("val"));
        element.ConvertToText().Should().BeEmpty();
    }

    [Fact]
    public void TableElement_ConvertsToText()
    {
        var cell1 = new TableCellElement("| cell1", "", new TextElement("cell1"), false);
        var cell2 = new TableCellElement("! cell2", "", new TextElement("cell2"), true);
        var row = new TableRowElement("|-", "", new[] { cell1, cell2 });
        var table = new TableElement("{|", "", new[] { row });
        table.ConvertToText().Should().Be("| cell1 | cell2 |\n\n");
    }
}