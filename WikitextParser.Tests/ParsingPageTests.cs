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
            "{{Main|Main Article for Section 1}}\n" + // Added main article link
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
        page.Infobox.Should().NotBeNull();
        page.Sections.Should().HaveCount(2);

        // Check Section 1
        var section1 = page.Sections[0];
        section1.Heading.ChildElement.ConvertToText().Should().Be("Section 1");

        section1.MainArticleLinks.Should().ContainSingle();
        var mainLinkTemplate = section1.MainArticleLinks.First();
        mainLinkTemplate.TemplateName.Should().Be("Main");
        mainLinkTemplate.Parameters.Should().ContainSingle();
        mainLinkTemplate.Parameters.First().Value.ConvertToText().Should().Be("Main Article for Section 1");

        // Check that the main article link is NOT in the regular content
        section1.ContentElements.Should().ContainSingle()
            .Which.Should().BeOfType<ParagraphElement>();
        section1.Subsections.Should().HaveCount(2);

        // Check Subsection 1.1 (and the rest of the test)
        var subsection1_1 = section1.Subsections[0];
        subsection1_1.Heading.ChildElement.ConvertToText().Should().Be("Subsection 1.1");
        subsection1_1.ContentElements.Should().ContainSingle()
            .Which.Should().BeOfType<TableElement>();
    }
}