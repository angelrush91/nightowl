using FluentAssertions;
using Nightowl.Infrastructure.ExternalServices;
using Xunit;

namespace Nightowl.Tests.Infrastructure;

public class OpenLibraryParserTests
{
    [Fact]
    public void ParseSearchResult_ShouldExtractAllMetadataCorrectly()
    {
        var json = """
        {
            "numFound": 1,
            "docs": [
                {
                    "title": "Clean Code",
                    "subtitle": "A Handbook of Agile Software Craftsmanship",
                    "author_name": ["Robert C. Martin"],
                    "publisher": ["Prentice Hall"],
                    "publish_year": [2008],
                    "number_of_pages_median": 464,
                    "cover_i": 8065615
                }
            ]
        }
        """;

        var result = OpenLibraryIsbnService.ParseSearchResult(json, "9780132350884");

        result.Should().NotBeNull();
        result!.Title.Should().Be("Clean Code");
        result.Subtitle.Should().Be("A Handbook of Agile Software Craftsmanship");
        result.Authors.Should().Be("Robert C. Martin");
        result.PageCount.Should().Be(464);
        result.Publisher.Should().Be("Prentice Hall");
        result.PublishDate.Should().Be("2008");
        result.CoverUrl.Should().Be("https://covers.openlibrary.org/b/id/8065615-M.jpg");
    }

    [Fact]
    public void ParseSearchResult_WithMinimalData_ShouldProvideSafeDefaults()
    {
        var json = """
        {
            "numFound": 1,
            "docs": [
                {
                    "title": "Minimal Book"
                }
            ]
        }
        """;

        var result = OpenLibraryIsbnService.ParseSearchResult(json, "9780132350884");

        result.Should().NotBeNull();
        result!.Title.Should().Be("Minimal Book");
        result.Authors.Should().Be("Unknown Author");
        result.PageCount.Should().Be(0);
        result.CoverUrl.Should().Be("https://covers.openlibrary.org/b/isbn/9780132350884-M.jpg");
    }

    [Fact]
    public void ParseEditionResult_ShouldExtractEditionMetadata()
    {
        var json = """
        {
            "title": "The Pragmatic Programmer",
            "number_of_pages": 352,
            "publishers": ["Addison-Wesley"],
            "publish_date": "October 1999",
            "description": "Straight from the programming trenches."
        }
        """;

        var result = OpenLibraryIsbnService.ParseEditionResult(json, "9780201616224");

        result.Should().NotBeNull();
        result!.Title.Should().Be("The Pragmatic Programmer");
        result.PageCount.Should().Be(352);
        result.Publisher.Should().Be("Addison-Wesley");
        result.PublishDate.Should().Be("October 1999");
        result.Description.Should().Be("Straight from the programming trenches.");
    }
}
