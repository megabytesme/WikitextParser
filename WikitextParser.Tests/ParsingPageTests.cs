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

    [Fact]
    public void ParsePage_FullFictionalArticle_BuildsCorrectAndCompleteHierarchy()
    {
        // Arrange
        var wikitext = SampleTextLoader.LoadFictionalPage();

        // Act
        var page = Parser.ParsePage(wikitext);

        // Assert
        page.Should().NotBeNull();

        // 1. Check Infobox
        page.Infobox.Should().NotBeNull();
        page.Infobox!.TemplateName.Should().Be("Infobox television");
        var creatorParam = page.Infobox.Parameters.FirstOrDefault(p => p.Key == "creator");
        creatorParam.Should().NotBeNull();
        creatorParam.Value.Should().BeOfType<LinkElement>()
            .Which.Url.Should().Be("The Weaver Siblings");

        // 2. Check Lead Content
        page.LeadContent.Should().HaveCount(4); // 3 metadata templates + 1 paragraph
        page.LeadContent.OfType<ParagraphElement>().Should().ContainSingle()
            .Which.ConvertToText().Should().StartWith("'Eerie Hollow' is a fictional American television series");

        // 3. Check Top-Level Sections
        page.Sections.Should().HaveCount(5);
        page.Sections.Select(s => s.Heading.ChildElement.ConvertToText()).Should().ContainInOrder(
            "Plot Overview", "Cast and Characters", "Production", "Reception", "Other Media"
        );

        // 4. Spot Check "Plot Overview" Section
        var overviewSection = page.Sections[0];
        overviewSection.Heading.HeadingLevel.Should().Be(2);
        overviewSection.ContentElements.Should().HaveCount(2); // Two paragraphs
        overviewSection.ContentElements.Last().As<ParagraphElement>()
            .ChildElements.OfType<RefElement>().Should().ContainSingle();

        // 5. Spot Check "Cast and Characters" Section
        var castSection = page.Sections[1];
        castSection.MainArticleLinks.Should().ContainSingle()
            .Which.TemplateName.Should().Be("Main");
        // The bulleted list is parsed as a single paragraph in the current implementation
        castSection.ContentElements.Should().ContainSingle()
            .Which.ConvertToText().Should().Contain("* [[Jane Doe]] as Sarah Vance");

        // 6. Spot Check "Production" Section and its Subsections
        var productionSection = page.Sections[2];
        productionSection.Subsections.Should().HaveCount(2);
        productionSection.Subsections[0].Heading.ChildElement.ConvertToText().Should().Be("Development");
        productionSection.Subsections[1].Heading.ChildElement.ConvertToText().Should().Be("Writing");
        productionSection.Subsections[0].ContentElements.Should().HaveCount(2); // Two paragraphs

        // 7. Spot Check "Other Media" Section for the Table
        var otherMediaSection = page.Sections.Last();
        var comicsSubsection = otherMediaSection.Subsections.Should().ContainSingle().Subject;
        comicsSubsection.Heading.ChildElement.ConvertToText().Should().Be("Comics");

        var table = comicsSubsection.ContentElements.OfType<TableElement>().Should().ContainSingle().Subject;
        table.Rows.Should().HaveCount(2); // Header row + 2 data rows

        // Check header row of the table
        var headerRow = table.Rows[0];
        headerRow.Cells.Should().HaveCount(3);
        headerRow.Cells[0].ConvertToText().Should().Be("Title");

        // Check first data row of the table
        var dataRow = table.Rows[1];
        dataRow.Cells.Should().HaveCount(3);
        dataRow.Cells[0].ConvertToText().Should().Be("'Eerie Hollow: The Other Side'");
    }
}