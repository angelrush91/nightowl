using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Nightowl.Application.Interfaces;
using Nightowl.Infrastructure.ExternalServices;
using Xunit;

namespace Nightowl.Tests.Infrastructure;

public class LiveOpenLibraryApiTests
{
    [Fact]
    public async Task ResolveAsync_WithCleanCodeIsbn_ShouldReturnValidMetadataFromLiveApi()
    {
        // Arrange
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(15);
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NightowlTests/1.0 (Testing ISBN API; contact: test@nightowl.app)");
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var resolver = new BookMetadataResolver(httpClient, memoryCache, NullLogger<BookMetadataResolver>.Instance);

        // Act - ISBN for Clean Code by Robert C. Martin
        var resolution = await resolver.ResolveAsync("9780132350884");

        // Assert
        resolution.Status.Should().Be(ResolutionStatus.Success);
        resolution.Metadata.Should().NotBeNull();
        resolution.Metadata!.Title.Should().Contain("Clean Code");
        resolution.Metadata.Authors.Should().Contain("Martin");
        resolution.Metadata.Isbn.Should().Be("9780132350884");
        resolution.Metadata.CoverUrl.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ResolveAsync_WithPragmaticProgrammerIsbn_ShouldReturnValidMetadataFromLiveApi()
    {
        // Arrange
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(15);
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NightowlTests/1.0 (Testing ISBN API; contact: test@nightowl.app)");
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var resolver = new BookMetadataResolver(httpClient, memoryCache, NullLogger<BookMetadataResolver>.Instance);

        // Act - ISBN for The Pragmatic Programmer
        var resolution = await resolver.ResolveAsync("9780201616224");

        // Assert
        resolution.Status.Should().Be(ResolutionStatus.Success);
        resolution.Metadata.Should().NotBeNull();
        resolution.Metadata!.Title.Should().Contain("Pragmatic Programmer");
        resolution.Metadata.Isbn.Should().Be("9780201616224");
    }
}
