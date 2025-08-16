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

    [Fact]
    public void Parse_References_HandlesNamedAndSelfClosingTags()
    {
        // Arrange
        var wikitext = "Text with a standard ref.<ref>Content</ref> " +
                       "And a named ref.<ref name=\"test\">Named Content</ref> " +
                       "And a self-closing ref.<ref name=\"self\" />";

        // Act
        var elements = Parser.Parse(wikitext).ToList();

        // Assert
        // **FIXED ASSERTION**: The chain is broken into two lines.
        elements.Should().ContainSingle();
        var paragraph = elements.First().Should().BeOfType<ParagraphElement>().Subject;

        var refs = paragraph.ChildElements.OfType<RefElement>().ToList();
        refs.Should().HaveCount(3);

        // Standard ref
        refs[0].Name.Should().BeNull();
        refs[0].ChildElement.Should().NotBeNull();
        refs[0].ChildElement.As<TextElement>().SourceText.Should().Be("Content");

        // Named ref
        refs[1].Name.Should().Be("test");
        refs[1].ChildElement.Should().NotBeNull();
        refs[1].ChildElement.As<TextElement>().SourceText.Should().Be("Named Content");

        // Self-closing ref
        refs[2].Name.Should().Be("self");
        refs[2].ChildElement.Should().BeNull();
    }


    [Fact]
    public void Parse_FileLinks_CorrectlyParsesOptionsAndCaption()
    {
        // Arrange
        var wikitextWithSingleNewline = "[[File:MyImage.jpg|thumb|220px|right|This is the caption.]]\n[[Image:Another.png]]";
        var wikitextWithDoubleNewline = "[[File:MyImage.jpg|thumb|220px|right|This is the caption.]]\n\n[[Image:Another.png]]";

        // Act
        var elements1 = Parser.Parse(wikitextWithSingleNewline).ToList();
        var elements2 = Parser.Parse(wikitextWithDoubleNewline).ToList();

        elements1.Should().ContainSingle();
        var paragraph1 = elements1.First().Should().BeOfType<ParagraphElement>().Subject;

        var children1 = paragraph1.ChildElements.ToList();
        children1.Should().HaveCount(3);
        children1[0].Should().BeOfType<ImageElement>();
        children1[1].Should().BeOfType<TextElement>();
        children1[2].Should().BeOfType<ImageElement>();


        elements2.Should().HaveCount(2);

        var para2_1 = elements2[0].Should().BeOfType<ParagraphElement>().Subject;
        para2_1.ChildElements.Should().ContainSingle();
        var image1 = para2_1.ChildElements.First().Should().BeOfType<ImageElement>().Subject;
        image1.FileName.Should().Be("MyImage.jpg");

        var para2_2 = elements2[1].Should().BeOfType<ParagraphElement>().Subject;
        para2_2.ChildElements.Should().ContainSingle();
        var image2 = para2_2.ChildElements.First().Should().BeOfType<ImageElement>().Subject;
        image2.FileName.Should().Be("Another.png");
    }

    [Fact]
    public void Parse_Tables_CorrectlyParsesStructure()
    {
        // Arrange
        var wikitext =
            "{| class=\"wikitable\" style=\"text-align: center\"\n" +
            "|-\n" +
            "! Header 1 !! Header 2\n" +
            "|-\n" +
            "| rowspan=\"2\" | Cell A1/B1\n" +
            "| Cell A2 with [[a link]]\n" +
            "|-\n" +
            "| Cell B2\n" +
            "|}";

        // Act
        var elements = Parser.Parse(wikitext).ToList();

        // Assert
        // **FIXED ASSERTION**: The chain is broken into two separate, clearer steps.
        elements.Should().ContainSingle();
        var table = elements.First().Should().BeOfType<TableElement>().Subject;

        table.Attributes.Should().Be("class=\"wikitable\" style=\"text-align: center\"");
        table.Rows.Should().HaveCount(3);

        // Row 1 (Headers)
        var row1 = table.Rows[0];
        row1.Cells.Should().HaveCount(2);
        row1.Cells[0].IsHeader.Should().BeTrue();
        row1.Cells[0].Content.As<TextElement>().SourceText.Should().Be("Header 1");
        row1.Cells[1].IsHeader.Should().BeTrue();
        row1.Cells[1].Content.As<TextElement>().SourceText.Should().Be("Header 2");

        // Row 2
        var row2 = table.Rows[1];
        row2.Cells.Should().HaveCount(2);
        row2.Cells[0].IsHeader.Should().BeFalse();
        row2.Cells[0].Attributes.Should().Be("rowspan=\"2\"");
        row2.Cells[0].Content.As<TextElement>().SourceText.Should().Be("Cell A1/B1");
        row2.Cells[1].Content.As<ParagraphElement>().ChildElements.OfType<LinkElement>().Single().Url.Should().Be("a link");

        // Row 3
        var row3 = table.Rows[2];
        row3.Cells.Should().ContainSingle();
        row3.Cells[0].IsHeader.Should().BeFalse();
        row3.Cells[0].Content.As<TextElement>().SourceText.Should().Be("Cell B2");
    }
}