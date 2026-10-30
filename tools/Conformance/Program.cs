using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using WikitextParser;

// Read-only comparisons against the actual wiki parser. No edit, login or token requests.
string endpoint = args.Length > 0 ? args[0] : "https://www.mediawiki.org/w/api.php";
if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var api) || api.Scheme != "https") throw new ArgumentException("Supply an HTTPS MediaWiki API endpoint.");
var cases = new[] {
    new Fixture("formatting", "A '''bold''' and ''italic'' phrase."),
    new Fixture("nested-formatting", "'''Bold with ''italic'' inside'''"),
    new Fixture("entities", "&lt; &gt; &amp; &#169; &#x3b1;"),
    new Fixture("literal", "<nowiki>[[Page]] {{T}} '''literal'''</nowiki>"),
    new Fixture("links", "[[Help:Contents|Contents]] and [[Book]]s"),
    new Fixture("lists", "* First\n** Nested\n* Last"),
    new Fixture("definition-list", "; Term : Definition\n: More"),
    new Fixture("table", "{| class=\"wikitable\"\n|+ Caption\n! Header !! Second\n|-\n| One || Two\n|}"),
    new Fixture("inclusion-page", "A<noinclude>B</noinclude><includeonly>C</includeonly><onlyinclude>D</onlyinclude>"),
    new Fixture("comments", "A<!-- hidden -->B"),
    new Fixture("template-runtime", "{{#expr:2+3}}", true),
    new Fixture("citations", "A<ref>Reference</ref><references/>", true)
};
using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(40) };
client.DefaultRequestHeaders.UserAgent.ParseAdd("WikitextParserConformance/0.2 (https://github.com/megabytesme/WikitextParser)");
var results = new List<object>(); int unexpected = 0, unavailable = 0;
foreach (var fixture in cases)
{
    string local = Text(string.Concat(Parser.Parse(fixture.Source).Select(e => e.ConvertToHtml())));
    try
    {
        using var response = await client.PostAsync(api, new FormUrlEncodedContent(new Dictionary<string, string> {
            ["action"] = "parse", ["format"] = "json", ["formatversion"] = "2", ["prop"] = "text", ["text"] = fixture.Source,
            ["title"] = "Project:Parser conformance preview", ["contentmodel"] = "wikitext", ["disableeditsection"] = "1", ["disablelimitreport"] = "1"
        }));
        response.EnsureSuccessStatusCode();
        using var parsed = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (parsed.RootElement.TryGetProperty("error", out var error)) throw new InvalidOperationException(error.ToString());
        string server = Text(parsed.RootElement.GetProperty("parse").GetProperty("text").GetString());
        bool match = local == server;
        if (!match && !fixture.KnownGap) unexpected++;
        results.Add(new { fixture.Name, fixture.Source, Local = local, Server = server, Match = match, fixture.KnownGap });
    }
    catch (Exception e) { unavailable++; results.Add(new { fixture.Name, Error = e.Message }); }
}
string report = JsonSerializer.Serialize(new { Endpoint = api.AbsoluteUri, CheckedUtc = DateTime.UtcNow, UnexpectedDifferences = unexpected, Unavailable = unavailable, Cases = results }, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(report);
if (args.Length > 1) await File.WriteAllTextAsync(args[1], report);
return unavailable > 0 ? 2 : unexpected > 0 ? 1 : 0;

static string Text(string html)
{
    var document = new HtmlDocument(); document.LoadHtml(html ?? "");
    foreach (var node in document.DocumentNode.SelectNodes("//script|//style|//*[contains(concat(' ',normalize-space(@class),' '),' mw-editsection ')]")?.ToList() ?? new()) node.Remove();
    foreach (var node in document.DocumentNode.Descendants().Where(n => new[] { "p", "li", "dt", "dd", "td", "th", "caption", "tr", "h1", "h2", "h3", "pre" }.Contains(n.Name)).ToList())
    { node.PrependChild(document.CreateTextNode(" ")); node.AppendChild(document.CreateTextNode(" ")); }
    return Regex.Replace(HtmlEntity.DeEntitize(document.DocumentNode.InnerText), @"\s+", " ").Trim();
}
record Fixture(string Name, string Source, bool KnownGap = false);
