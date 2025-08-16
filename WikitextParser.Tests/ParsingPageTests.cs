using AwesomeAssertions;
using WikitextParser.Elements;

namespace WikitextParser.Tests;

public class ParsingPageTests
{
    [Fact]
    public void ParsePage_BuildsCorrectHierarchy()
    {
        // Arrange
        var wikitext =
            "{{Infobox test|name=Test}}\n" +
            "This is the lead paragraph.\n\n" +
            "== Section 1 ==\n" +
            "Content in section 1.\n" +
            "=== Subsection 1.1 ===\n" +
            "{| \n| Cell\n|}\n" +
            "=== Subsection 1.2 ===\n" +
            "Content in subsection 1.2.\n" +
            "== Section 2 ==\n" +
            "Final paragraph.";

        // Act
        var page = Parser.ParsePage(wikitext);

        // Assert
        page.Should().NotBeNull();

        // Check Infobox
        page.Infobox.Should().NotBeNull();
        page.Infobox!.TemplateName.Should().Be("Infobox test");

        // Check Lead Content
        page.LeadContent.Should().ContainSingle()
            .Which.Should().BeOfType<ParagraphElement>();

        // Check Top-Level Sections
        page.Sections.Should().HaveCount(2);

        // Check Section 1
        var section1 = page.Sections[0];
        section1.Heading.ChildElement.ConvertToText().Should().Be("Section 1");
        section1.ContentElements.Should().ContainSingle();
        section1.ContentElements.Single().Should().BeOfType<ParagraphElement>();
        section1.Subsections.Should().HaveCount(2);

        // Check Subsection 1.1
        var subsection1_1 = section1.Subsections[0];
        subsection1_1.Heading.HeadingLevel.Should().Be(3);
        subsection1_1.Heading.ChildElement.ConvertToText().Should().Be("Subsection 1.1");
        subsection1_1.ContentElements.Should().ContainSingle()
            .Which.Should().BeOfType<TableElement>();
        subsection1_1.Subsections.Should().BeEmpty();

        // Check Subsection 1.2
        var subsection1_2 = section1.Subsections[1];
        subsection1_2.Heading.ChildElement.ConvertToText().Should().Be("Subsection 1.2");
        subsection1_2.ContentElements.Should().ContainSingle()
            .Which.Should().BeOfType<ParagraphElement>();

        // Check Section 2
        var section2 = page.Sections[1];
        section2.Heading.ChildElement.ConvertToText().Should().Be("Section 2");
        section2.ContentElements.Should().ContainSingle();
        section2.Subsections.Should().BeEmpty();
    }
}