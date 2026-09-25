using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nightowl.Infrastructure.ExternalServices;
using Xunit;

namespace Nightowl.Tests.Infrastructure;

public class LiveOpenLibraryApiTests
{
    [Fact]
    public async Task LookupByIsbnAsync_WithCleanCodeIsbn_ShouldReturnValidMetadataFromLiveApi()
    {
        // Arrange
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(15);
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NightowlTests/1.0 (Testing ISBN API; contact: test@nightowl.app)");
        var service = new OpenLibraryIsbnService(httpClient, NullLogger<OpenLibraryIsbnService>.Instance);

        // Act - ISBN for Clean Code by Robert C. Martin
        var metadata = await service.LookupByIsbnAsync("9780132350884");

        // Assert
        metadata.Should().NotBeNull();
        metadata!.Title.Should().Contain("Clean Code");
        metadata.Authors.Should().Contain("Martin");
        metadata.Isbn.Should().Be("9780132350884");
        metadata.CoverUrl.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task LookupByIsbnAsync_WithPragmaticProgrammerIsbn_ShouldReturnValidMetadataFromLiveApi()
    {
        // Arrange
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(15);
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NightowlTests/1.0 (Testing ISBN API; contact: test@nightowl.app)");
        var service = new OpenLibraryIsbnService(httpClient, NullLogger<OpenLibraryIsbnService>.Instance);

        // Act - ISBN for The Pragmatic Programmer
        var metadata = await service.LookupByIsbnAsync("9780201616224");

        // Assert
        metadata.Should().NotBeNull();
        metadata!.Title.Should().Contain("Pragmatic Programmer");
        metadata.Isbn.Should().Be("9780201616224");
    }
}
