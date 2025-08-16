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

        var infoboxParams = infobox.Parameters.ToList(); // Use Parameters property
        infoboxParams.Should().HaveCount(12);

        // Check a simple key-value pair (country)
        var countryParam = infoboxParams.FirstOrDefault(p => p.Key == "country");
        countryParam.Should().NotBeNull();
        countryParam.Value.As<TextElement>().SourceText.Should().Be("United States");
        
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

    [Fact]
    public void Parse_Headings_CorrectlyParsesSimpleAndItalicHeadings()
    {
        // Arrange
        var wikitext = "==Simple Heading==\n" +
                       "=== ''Italic Heading'' ===";

        // Act
        var elements = Parser.Parse(wikitext).ToList();

        // Assert
        elements.Should().HaveCount(2);

        // Test simple H2 heading
        elements[0].Should().BeOfType<HeadingElement>().Which.HeadingLevel.Should().Be(2);
        elements[0].As<HeadingElement>().ChildElement.Should().BeOfType<TextElement>()
            .Which.SourceText.Should().Be("Simple Heading");

        // Test italic H3 heading
        elements[1].Should().BeOfType<HeadingElement>().Which.HeadingLevel.Should().Be(3);
        elements[1].As<HeadingElement>().ChildElement.Should().BeOfType<ItalicElement>()
            .Which.InnerElement.Should().BeOfType<TextElement>()
            .Which.SourceText.Should().Be("Italic Heading");
    }

    [Fact]
    public void Parse_CategoryLinks_CorrectlyParsesCategories()
    {
        // Arrange
        var wikitext = "[[Category:Stranger Things (TV series)| ]]\n" +
                       "[[Category:2010s American drama television series]]";

        // Act
        var elements = Parser.Parse(wikitext).ToList();

        // Assert
        elements.Should().HaveCount(2);

        // Test category with sort key
        elements[0].Should().BeOfType<CategoryElement>()
            .Which.CategoryName.Should().Be("Stranger Things (TV series)");
        elements[0].As<CategoryElement>().SortKey.Should().Be(" ");

        // Test category without sort key
        elements[1].Should().BeOfType<CategoryElement>()
            .Which.CategoryName.Should().Be("2010s American drama television series");
        elements[1].As<CategoryElement>().SortKey.Should().BeNull();
    }
    [Fact]
    public void Parse_AdvancedTemplates_CorrectlyParsesParameters()
    {
        // Arrange
        var wikitext = "{{Main|Article 1|Article 2|l1=Custom Label 1}}\n" +
                       "{{:List of episodes}}\n" +
                       "{{Quote box|quote=Some text with a [[link|pipe]].|source=A source}}";

        // Act
        var elements = Parser.Parse(wikitext).ToList();

        // Assert
        elements.Should().HaveCount(3);

        // 1. Test {{Main}} template with positional and named parameters
        var mainTemplate = elements[0].Should().BeOfType<TemplateElement>().Subject;
        mainTemplate.TemplateName.Should().Be("Main");
        mainTemplate.Parameters.Should().HaveCount(3);

        var mainParams = mainTemplate.Parameters.ToList();
        mainParams[0].Key.Should().BeNull();
        mainParams[0].Value.As<TextElement>().SourceText.Should().Be("Article 1");
        mainParams[1].Key.Should().BeNull();
        mainParams[1].Value.As<TextElement>().SourceText.Should().Be("Article 2");
        mainParams[2].Key.Should().Be("l1");
        mainParams[2].Value.As<TextElement>().SourceText.Should().Be("Custom Label 1");

        // 2. Test transclusion template
        var transclusionTemplate = elements[1].Should().BeOfType<TemplateElement>().Subject;
        transclusionTemplate.TemplateName.Should().Be(":List of episodes");
        transclusionTemplate.IsTransclusion.Should().BeTrue();
        transclusionTemplate.Parameters.Should().BeEmpty();

        // 3. Test template with a link (containing a pipe) in a parameter
        var quoteTemplate = elements[2].Should().BeOfType<TemplateElement>().Subject;
        quoteTemplate.TemplateName.Should().Be("Quote box");
        var quoteParams = quoteTemplate.Parameters.ToList();
        quoteParams[0].Key.Should().Be("quote");
        var quoteValueParagraph = quoteParams[0].Value.Should().BeOfType<ParagraphElement>().Subject;

        quoteValueParagraph.ChildElements.OfType<LinkElement>().Single()
            .DisplayText.Should().Be("link");
    }
}