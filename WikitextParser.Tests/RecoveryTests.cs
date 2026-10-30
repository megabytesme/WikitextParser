using WikitextParser.Elements;

namespace WikitextParser.Tests;

public class RecoveryTests
{
    [Fact(Timeout = 20000)]
    public Task MixedMalformedInputAlwaysMakesProgress() => Task.Run(() =>
    {
        var random = new Random(20261030);
        string[] tokens = { "text", "\n", "\n\n", "{{", "}}", "{{{", "}}}", "[[", "]]", "|", "=", "<ref name=>", "</ref>", "<nowiki>", "</nowiki>", "<!--", "-->", "'''", "''", "*", ":", "{|", "|}", "<span>", "</span>" };
        for (int sample = 0; sample < 200; sample++)
        {
            string source = string.Concat(Enumerable.Range(0, 40).Select(_ => tokens[random.Next(tokens.Length)]));
            var elements = Parser.Parse(source).Take(source.Length + 1).ToList();
            Assert.True(elements.Count <= source.Length);
            Assert.All(elements, element => Assert.False(string.IsNullOrEmpty(element.SourceText)));
            foreach (var element in elements) { _ = element.ConvertToHtml(); _ = element.ConvertToText(); }
        }
    });

    [Fact]
    public void ExcessiveInlineNestingFallsBackToLiteralText()
    {
        string source = string.Concat(Enumerable.Repeat("<span>", 500)) + "retained" + string.Concat(Enumerable.Repeat("</span>", 500));
        var document = Parser.ParseDocument(source);
        Assert.Contains("retained", string.Concat(document.Elements.Select(e => e.ConvertToText())));
        Assert.True(ElementTraversal.DescendantsAndSelf(document.Elements).Count() < 200);
    }
}
