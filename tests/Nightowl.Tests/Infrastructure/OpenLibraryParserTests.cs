using System.Text.Json;
using FluentAssertions;
using Nightowl.Infrastructure.ExternalServices;
using Xunit;

namespace Nightowl.Tests.Infrastructure;

public class OpenLibraryParserTests
{
    [Fact]
    public void ParseOpenLibraryElement_ShouldExtractAllMetadataCorrectly()
    {
        // Sample JSON response from OpenLibrary for Clean Code
        var json = """
        {
            "title": "Clean Code",
            "subtitle": "A Handbook of Agile Software Craftsmanship",
            "authors": [
                { "name": "Robert C. Martin", "url": "https://openlibrary.org/authors/OL2632070A/Robert_C._Martin" }
            ],
            "number_of_pages": 464,
            "publishers": [
                { "name": "Prentice Hall" }
            ],
            "publish_date": "August 1, 2008",
            "cover": {
                "small": "https://covers.openlibrary.org/b/id/8231990-S.jpg",
                "medium": "https://covers.openlibrary.org/b/id/8231990-M.jpg",
                "large": "https://covers.openlibrary.org/b/id/8231990-L.jpg"
            },
            "description": "Even bad code can function. But if code isn't clean, it can bring a development organization to its knees."
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var result = OpenLibraryIsbnService.ParseOpenLibraryElement(doc.RootElement, "9780132350884");

        result.Should().NotBeNull();
        result.Title.Should().Be("Clean Code");
        result.Subtitle.Should().Be("A Handbook of Agile Software Craftsmanship");
        result.Authors.Should().Be("Robert C. Martin");
        result.PageCount.Should().Be(464);
        result.Publisher.Should().Be("Prentice Hall");
        result.PublishDate.Should().Be("August 1, 2008");
        result.CoverUrl.Should().Be("https://covers.openlibrary.org/b/id/8231990-L.jpg");
        result.Description.Should().Contain("Even bad code can function");
    }

    [Fact]
    public void ParseOpenLibraryElement_WithMinimalData_ShouldProvideSafeDefaults()
    {
        var json = """
        {
            "title": "Minimal Book"
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var result = OpenLibraryIsbnService.ParseOpenLibraryElement(doc.RootElement, "9780132350884");

        result.Should().NotBeNull();
        result.Title.Should().Be("Minimal Book");
        result.Authors.Should().Be("Unknown Author");
        result.PageCount.Should().Be(0);
        result.CoverUrl.Should().Be("https://covers.openlibrary.org/b/isbn/9780132350884-M.jpg");
    }
}
