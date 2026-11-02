# WikitextParser

WikitextParser is a platform-independent .NET Standard 1.4 library for parsing MediaWiki wikitext into a structured object model. It provides an ordered syntax tree, page and section APIs, and basic HTML and plain-text conversion.

Use it in .NET MAUI, console applications, web services, or other applications running on a compatible .NET runtime. The library has no dependency on a UI framework. Applications choose how to obtain the wikitext and how to display or process the parsed content. See Microsoft's [.NET Standard documentation](https://learn.microsoft.com/en-us/dotnet/standard/net-standard) for runtime compatibility.

The parser covers core syntax; it does not implement the complete MediaWiki runtime. Template expansion, Lua, extensions and exact wiki rendering require context from the originating wiki or application-provided handlers.

## Wiki-specific syntax

Wiki-specific syntax can be supplied without changing process-global state:

```csharp
var options = new ParserOptions {
    FileNamespaces = new[] { "Datei", "Bild" },
    CategoryNamespaces = new[] { "Kategorie" },
    RedirectAliases = new[] { "#WEITERLEITUNG" },
    LinkTrailPattern = "[a-zäöüß]+", // .NET fragment; not MediaWiki's PCRE wrapper
    InclusionMode = InclusionMode.Page
};
var document = Parser.ParseDocument(source, options);
// SourceText preserves the original; PreprocessedText shows inclusion selection.
```

Use `InclusionMode.Transclusion` for inclusion controls when inspecting template bodies, or `Preserve` to retain their markup. This selects content; template expansion, substitution, Lua, extension behavior and full MediaWiki grammar remain with the originating wiki.

## Features

- **Dual-Level API**:
    - **High-Level API (`Parser.ParsePage`)**: Parses an entire page into a structured `Page` object with a lead section, an infobox, and a nested hierarchy of sections.
    - **Low-Level API (`Parser.Parse`)**: Parses wikitext into a flat `IEnumerable<WikitextElement>` for custom processing and analysis.
- **Content Conversion**: Convert any parsed element, section, or the entire page into either HTML or plain text.
- **Platform Independence**: Targets .NET Standard 1.4 and can be consumed by compatible .NET applications without a UI framework dependency.
- **Document Inspection**: `Parser.ParseDocument` retains source order and reports known constructs requiring additional wiki context or application handling. `ElementTraversal` exposes children for custom analysis, conversion or rendering.

## Installation

This library will be available via NuGet. You can install it using the .NET CLI:

```bash
dotnet add package WikitextParser
```

Or through the NuGet Package Manager in Visual Studio.

## Usage Guide

Using the parser is straightforward. The recommended approach is to use the high-level `ParsePage` API for most use cases.

### High-Level API: Parsing a Page

This is the easiest way to get a structured representation of a wiki page.

```csharp
using WikitextParser;
using WikitextParser.Models;

string wikitext = File.ReadAllText("my_wikitext_file.txt");

// 1. Parse the entire page into a Page object
Page page = Parser.ParsePage(wikitext);

// 2. Access the Infobox
if (page.Infobox != null)
{
    var creatorParam = page.Infobox.Parameters.FirstOrDefault(p => p.Key == "creator");
    if (creatorParam != null)
    {
        Console.WriteLine($"Creator: {creatorParam.Value.ConvertToText()}");
    }
}

// 3. Access the Lead Content (content before the first heading)
foreach (var element in page.LeadContent)
{
    // Ignores metadata templates like {{Short description|...}}
    if (element is not WikiKeyValuePairElement)
    {
        Console.WriteLine(element.ConvertToText());
    }
}

// 4. Recursively process all sections and subsections
void PrintSection(Section section, int indent = 0)
{
    string indentStr = new string(' ', indent * 2);
    Console.WriteLine($"{indentStr}{section.Heading.ConvertToText().Trim()}");

    // Print main article links for the section
    foreach (var mainLink in section.MainArticleLinks)
    {
        Console.WriteLine($"{indentStr}  (See also: {mainLink.Parameters.First().Value.ConvertToText()})");
    }

    // Print the section's direct content
    foreach(var content in section.ContentElements)
    {
        Console.WriteLine($"{indentStr}  [Content: {content.GetType().Name}]");
    }
    
    // Recurse into subsections
    foreach (var sub in section.Subsections)
    {
        PrintSection(sub, indent + 1);
    }
}

Console.WriteLine("\n--- PAGE STRUCTURE ---");
foreach (var section in page.Sections)
{
    PrintSection(section);
}

// 5. Convert the entire page to HTML or Text
string fullHtml = page.ConvertToHtml();
string fullText = page.ConvertToText();
```

### Low-Level API: Parsing Elements

If you need fine-grained control or want to process elements as a simple stream, you can use the low-level `Parse` method.

```csharp
using WikitextParser;
using WikitextParser.Elements;

string wikitext = "== Section 1 ==\nThis is ''italic'' text.\n\n[[Category:My Category]]";

var elements = Parser.Parse(wikitext);

foreach (var element in elements)
{
    switch (element.Type)
    {
        case WikitextElementType.Heading:
            var heading = (HeadingElement)element;
            Console.WriteLine($"Found H{heading.HeadingLevel}: {heading.ConvertToText().Trim()}");
            break;
            
        case WikitextElementType.Paragraph:
            var paragraph = (ParagraphElement)element;
            Console.WriteLine($"Found Paragraph: {paragraph.ConvertToText().Trim()}");
            break;
            
        case WikitextElementType.Category:
            var category = (CategoryElement)element;
            Console.WriteLine($"Found Category: {category.CategoryName}");
            break;
            
        default:
            Console.WriteLine($"Found other element type: {element.Type}");
            break;
    }
}
```

## Supported Elements

The parser currently supports the following wikitext syntax:

| Element | Wikitext Syntax | Notes |
|---|---|---|
| **Paragraphs** | `Some text.` | Paragraphs are separated by one or more blank lines (`\n\n`). Single newlines are treated as line breaks within a paragraph. |
| **Headings** | `= H1 =`, `== H2 ==`, etc. | Levels 1 through 6; page hierarchy retains skipped levels. |
| **Bold** | `'''Bold Text'''` | |
| **Italic** | `''Italic Text''` | |
| **Bold & Italic**| `'''''Bold Italic'''''` | |
| **Internal Links** | `[[Page Name]]`, `[[Page Name|Display Text]]` | |
| **Templates** | `{{TemplateName|param1|key=value}}` | Supports named and positional parameters, as well as nested templates. |
| **Infoboxes** | `{{Infobox ... }}` | Recognized as a special `TemplateElement` and separated in the high-level API. |
| **Simple KVP** | `{{Short description|...}}` | Simple single-line templates are parsed as `WikiKeyValuePairElement`. |
| **Tables** | `{| ... |}` | Captions, implicit rows, headers, cell attributes, continuation lines and nested tables; protected separators are retained. |
| **References** | `<ref>...</ref>`, `<ref name="..."/>`, `<ref name="...">...</ref>` | Parses standard, named, and self-closing reference tags. |
| **File/Image** | `[[File:Name.jpg|thumb|caption]]` | Parses filename, options (like `thumb`, `right`, `220px`), and the final caption. |
| **Category Links**| `[[Category:Category Name|Sort Key]]` | Parsed as distinct `CategoryElement` objects. |
| **HTML Comments**| `<!-- A comment -->` | Comments are parsed and preserved. |
| **Lists and Indentation** | `*`, `#`, `;`, `:` | Nested and mixed lists, definition terms and indentation. |
| **Preformatted Text** | Space-prefixed lines, `<pre>` | Space-prefixed lines allow inline markup; explicit pre blocks are literal. |
| **Literal Regions** | `<nowiki>...</nowiki>`, `<nowiki/>` | Protects markup and block boundaries. |
| **Entities** | `&amp;`, `&#65;`, `&#x41;` | Decoded for text, encoded once for HTML. |
| **External Links** | `[https://example.org label]`, bare URLs | HTTP(S), FTP, mailto and protocol-relative links; labels can contain markup. |
| **Formatting Tags** | `<sup>`, `<sub>`, `<small>`, `<span>`, etc. | Structured nodes; default HTML conversion allows a limited attribute set. |
| **Template Arguments** | `{{{name|default}}}` | Distinct nodes; unresolved until expanded by the host/wiki. |
| **Switches and Redirects** | `__TOC__`, `#REDIRECT [[Target]]` | Structured metadata, not full behavioral implementation. |
| **Extension Tags** | `<math>`, `<gallery>`, `<syntaxhighlight>`, etc. | Retained as opaque nodes and reported as needing a handler. |

### Inspecting the document tree

```csharp
var document = Parser.ParseDocument(wikitext);
foreach (var issue in document.CompatibilityIssues)
    Console.WriteLine($"{issue.Kind}: {issue.Element.SourceText}");

foreach (var element in ElementTraversal.DescendantsAndSelf(document.Elements))
    Console.WriteLine(element.Type);
```

An empty issue list means no *known* unresolved constructs were found; it is not a conformance certificate. `ParseDocument` keeps infoboxes in their original position, while `ParsePage` extracts the first infobox for analysis. Applications can traverse the tree for analysis, implement their own converters, or obtain server-expanded output for content that depends on templates, modules or extensions.

## Conversion to HTML and Text

Every parsed element (`WikitextElement`), as well as the high-level `Page` and `Section` objects, includes two methods: `ConvertToHtml()` and `ConvertToText()`.

- **`ConvertToHtml()`**: Produces a basic HTML representation of the content.
- **`ConvertToText()`**: Produces a plain text representation, stripping out all markup.

```csharp
Page page = Parser.ParsePage(wikitext);

// Convert the entire page
string fullHtml = page.ConvertToHtml();
string fullText = page.ConvertToText();
```

**Notice:** The built-in conversion methods are designed to be simple and provide a reasonable default output.
They are not customizable. 
For more advanced formatting, custom logic, or to handle specific templates in a unique way, 
it is recommended to traverse the parsed element tree and build your own custom conversion logic.


## Building from Source

To build the project yourself:

1.  Install dotnet sdk (9 or newer)
2.  Clone the repository.
3.  Navigate to the root directory.
4.  Run `dotnet build`.

The library targets .NET Standard 1.4 and uses C# 7.3. The test project uses .NET 9; run `dotnet test WikitextParser.sln -c Release` with that runtime installed, or set `DOTNET_ROLL_FORWARD=Major` when using a compatible newer runtime.

## License

This project is licensed under the MIT License.
