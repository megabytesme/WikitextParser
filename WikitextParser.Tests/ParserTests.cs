using AwesomeAssertions;
using WikitextParser.Elements;

namespace WikitextParser.Tests;

public class ParserTests
{
    [Fact]
    public void Parse_SampleWikitext_CorrectlyParsesStructure()
    {
        // Arrange
        var wikitext = SampleTextLoader.LoadSampleText1();

        // Act
        var elements = Parser.Parse(wikitext).ToList();

        // Assert
        // Check for top-level elements
        elements.Should().HaveCount(5);
        elements.Take(3).Should().AllBeOfType<WikiKeyValuePairElement>();
        elements[3].Should().BeOfType<TemplateElement>();
        elements[4].Should().BeOfType<ParagraphElement>();

        // Check the first key-value pair
        elements[0].As<WikiKeyValuePairElement>().Key.Should().Be("Short description");
        elements[0].As<WikiKeyValuePairElement>().Value.Should().BeOfType<TextElement>()
            .Which.SourceText.Should().Be("Fictional television series");

        // Inspect the Infobox
        var infobox = elements[3].As<TemplateElement>();
        infobox.TemplateName.Should().Be("Infobox television");
        infobox.IsInfobox.Should().BeTrue();

        var infoboxPairs = infobox.ChildElements.ToList();
        infoboxPairs.Should().HaveCount(12); // Corrected assertion from 11 to 12

        // Check a key-value pair with a comment
        var seasonsPair = infoboxPairs.First(p => p.Key == "num_seasons");
        seasonsPair.Value.Should().BeOfType<ParagraphElement>();
        var seasonChildren = seasonsPair.Value.As<ParagraphElement>().ChildElements.ToList();
        seasonChildren.Should().HaveCount(2);
        seasonChildren[0].Should().BeOfType<TextElement>().Which.SourceText.Should().Be("2");
        seasonChildren[1].Should().BeOfType<HtmlCommentElement>().Which.SourceText.Should().Be("<!--Do not increment until season is released!-->");

        // Check a simple key-value pair (country)
        var countryPair = infoboxPairs.First(p => p.Key == "country");
        countryPair.Should().NotBeNull();
        countryPair!.Value.As<TextElement>().SourceText.Should().Be("United States");

        // Inspect the Paragraph
        var paragraph = elements[4].As<ParagraphElement>();
        var paragraphChildren = paragraph.ChildElements.ToList();

        // '''''Mystery Grove'''''
        paragraphChildren[0].Should().BeOfType<ItalicElement>()
            .Which.InnerElement.Should().BeOfType<BoldElement>()
            .Which.InnerElement.Should().BeOfType<TextElement>()
            .Which.SourceText.Should().Be("Mystery Grove");

        // Check for the comment inside the paragraph
        var comment = paragraphChildren.FirstOrDefault(c => c.Type == WikitextElementType.Comment);
        comment.Should().NotBeNull();
        comment.Should().BeOfType<HtmlCommentElement>().Which.SourceText.Should().Be("<!--A third season is planned.-->");
    }
}