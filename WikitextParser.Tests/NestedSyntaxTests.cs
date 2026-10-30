using WikitextParser.Elements;

namespace WikitextParser.Tests;

public class NestedSyntaxTests
{
    [Fact]
    public void TemplateParametersRespectAllNestedSeparators()
    {
        var template = Assert.IsType<TemplateElement>(Assert.Single(Parser.Parse("{{T|{{Inner|a=b}}|[[A|B=C]]|<nowiki>a|b=c</nowiki>|<!--|=-->|name={{{arg|a=b}}}| |}}")));
        var parameters = template.Parameters.ToList();
        Assert.Equal(7, parameters.Count);
        Assert.Null(parameters[0].Key);
        Assert.IsType<TemplateElement>(parameters[0].Value);
        Assert.Null(parameters[1].Key);
        Assert.Equal("name", parameters[4].Key);
        var argument = Assert.IsType<TemplateArgumentElement>(parameters[4].Value);
        Assert.Equal("arg", argument.Name);
        Assert.Equal("a=b", argument.DefaultValue.ConvertToText());
        Assert.Equal(" ", parameters[5].Value.SourceText);
        Assert.Equal("", parameters[6].Value.SourceText);
    }

    [Fact]
    public void ArgumentCanNestAnotherArgumentAndTemplate()
    {
        var paragraph = Assert.IsType<ParagraphElement>(Assert.Single(Parser.Parse("{{{a|{{{b|{{Inner|one|two}}}}}}}}")));
        var outer = Assert.IsType<TemplateArgumentElement>(Assert.Single(paragraph.ChildElements));
        Assert.Equal("a", outer.Name);
        var inner = Assert.IsType<TemplateArgumentElement>(outer.DefaultValue);
        Assert.Equal("b", inner.Name);
        Assert.IsType<TemplateElement>(inner.DefaultValue);
    }

    [Fact]
    public void EmptyAndNumericNamedParametersRemainDistinct()
    {
        var template = Assert.IsType<TemplateElement>(Assert.Single(Parser.Parse("{{T| first ||1= override |last}}")));
        Assert.Equal(new string?[] { null, null, "1", null }, template.Parameters.Select(p => p.Key));
        Assert.Equal(new[] { " first ", "", "override", "last" }, template.Parameters.Select(p => p.Value.SourceText));
    }

    [Fact]
    public void PlainlistRecognitionIncludesUpstreamFixAndKeepsWholeItems()
    {
        var template = Assert.IsType<TemplateElement>(Assert.Single(Parser.Parse("{{plainlist|\n* before [[A|label]] after\n}}")));
        Assert.True(template.IsPlainlist);
        Assert.Equal("<ul><li>before <a href=\"/wiki/A\">label</a> after</li></ul>", template.ConvertToHtml());
    }

    [Fact]
    public void TableKeepsCaptionImplicitFirstRowAndContinuationLines()
    {
        var table = Assert.IsType<TableElement>(Assert.Single(Parser.Parse("{| class='wikitable'\n|+ title='caption' | A ''caption''\n! H1 !! H2\n|-\n| one\ncontinued\n| two\n|}")));
        Assert.Equal("A caption", table.Caption.ConvertToText());
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("one\ncontinued", table.Rows[1].Cells[0].Content.ConvertToText());
        Assert.Contains("<caption title=\"caption\">A <em>caption</em></caption>", table.ConvertToHtml());
        Assert.DoesNotContain("<p>", table.ConvertToHtml());
    }

    [Fact]
    public void TableDoesNotSplitProtectedPipesOrLoseNestedTables()
    {
        var table = Assert.IsType<TableElement>(Assert.Single(Parser.Parse("{|\n| title='a||b' | [[Target|label]] || <nowiki>a||b</nowiki>\n|-\n| before\n{|\n| nested\n|}\nafter\n|}")));
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal(2, table.Rows[0].Cells.Count);
        Assert.Equal("label", table.Rows[0].Cells[0].Content.ConvertToText());
        Assert.Equal("a||b", table.Rows[0].Cells[1].Content.ConvertToText());
        Assert.Contains("nested", table.Rows[1].Cells[0].Content.ConvertToText());
        Assert.Equal(2, Count(table.ConvertToHtml(), "<table"));
    }

    [Fact]
    public void FileCaptionCanContainLinksTemplatesAndLiteralPipes()
    {
        var paragraph = Assert.IsType<ParagraphElement>(Assert.Single(Parser.Parse("[[File:Test.png|thumb|200x100px|link=Target|before [[A|label]] after]]")));
        var image = Assert.IsType<ImageElement>(Assert.Single(paragraph.ChildElements));
        Assert.Equal("before [[A|label]] after", image.Caption);
        Assert.Contains("200x100px", image.Options);
        Assert.Contains("link=Target", image.Options);
    }

    [Fact]
    public void HtmlAttributesCannotInjectExecutableMarkup()
    {
        var table = Assert.IsType<TableElement>(Assert.Single(Parser.Parse("{| onclick='bad()' class='wikitable' style='background:url(x)'\n|- onmouseover='bad()'\n| title='a > b' onclick='bad()' | content\n|}")));
        string html = table.ConvertToHtml();
        Assert.DoesNotContain("onclick", html);
        Assert.DoesNotContain("onmouseover", html);
        Assert.DoesNotContain("style=", html);
        Assert.Contains("title=\"a &gt; b\"", html);
    }

    private static int Count(string text, string value) => (text.Length - text.Replace(value, "").Length) / value.Length;
}
