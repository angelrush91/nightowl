using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Nightowl.Application.Interfaces;
using Nightowl.Infrastructure.ExternalServices;
using Xunit;

namespace Nightowl.Tests.Infrastructure;

public class BookMetadataResolverTests
{
    private readonly IMemoryCache _memoryCache;

    public BookMetadataResolverTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-isbn")]
    [InlineData("12345")]
    [InlineData("9780132350889")] // Invalid checksum
    public async Task ResolveAsync_WithInvalidIsbn_ShouldReturnInvalidStatusWithoutCallingNetwork(string invalidIsbn)
    {
        var testHandler = new TestHttpMessageHandler();
        using var httpClient = new HttpClient(testHandler);
        var resolver = new BookMetadataResolver(httpClient, _memoryCache, NullLogger<BookMetadataResolver>.Instance);

        var result = await resolver.ResolveAsync(invalidIsbn);

        result.Status.Should().Be(ResolutionStatus.InvalidIsbn);
        result.Metadata.Should().BeNull();
        testHandler.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task ResolveAsync_WhenOpenLibrarySearchMatches_ShouldReturnSuccessAndCacheResult()
    {
        const string validIsbn = "9780132350884";
        var openLibrarySearchJson = """
        {
            "numFound": 1,
            "docs": [
                {
                    "title": "Clean Code",
                    "author_name": ["Robert C. Martin"],
                    "number_of_pages_median": 464,
                    "cover_i": 8065615
                }
            ]
        }
        """;

        var testHandler = new TestHttpMessageHandler();
        testHandler.SetResponse("openlibrary.org/search.json", HttpStatusCode.OK, openLibrarySearchJson);

        using var httpClient = new HttpClient(testHandler);
        var resolver = new BookMetadataResolver(httpClient, _memoryCache, NullLogger<BookMetadataResolver>.Instance);

        // First call: hits network
        var firstResult = await resolver.ResolveAsync(validIsbn);

        firstResult.Status.Should().Be(ResolutionStatus.Success);
        firstResult.Metadata.Should().NotBeNull();
        firstResult.Metadata!.Title.Should().Be("Clean Code");
        firstResult.Metadata.Authors.Should().Be("Robert C. Martin");
        testHandler.RequestCount.Should().Be(1);

        // Second call: served from memory cache (0 additional network calls)
        var secondResult = await resolver.ResolveAsync("978-0-13-235088-4"); // With hyphens

        secondResult.Status.Should().Be(ResolutionStatus.Success);
        secondResult.Metadata!.Title.Should().Be("Clean Code");
        testHandler.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task ResolveAsync_WhenOpenLibrarySearchEmpty_ShouldFallbackToOpenLibraryEdition()
    {
        const string validIsbn = "9780201616224";
        var emptySearchJson = "{\"numFound\": 0, \"docs\": []}";
        var editionJson = """
        {
            "title": "The Pragmatic Programmer",
            "number_of_pages": 352,
            "publishers": ["Addison-Wesley"]
        }
        """;

        var testHandler = new TestHttpMessageHandler();
        testHandler.SetResponse("openlibrary.org/search.json", HttpStatusCode.OK, emptySearchJson);
        testHandler.SetResponse("openlibrary.org/isbn/", HttpStatusCode.OK, editionJson);

        using var httpClient = new HttpClient(testHandler);
        var resolver = new BookMetadataResolver(httpClient, _memoryCache, NullLogger<BookMetadataResolver>.Instance);

        var result = await resolver.ResolveAsync(validIsbn);

        result.Status.Should().Be(ResolutionStatus.Success);
        result.Metadata.Should().NotBeNull();
        result.Metadata!.Title.Should().Be("The Pragmatic Programmer");
        result.Metadata.PageCount.Should().Be(352);
        testHandler.RequestCount.Should().Be(2); // Tried search, then edition
    }

    [Fact]
    public async Task ResolveAsync_WhenOpenLibraryFails_ShouldFallbackToGoogleBooks()
    {
        const string validIsbn = "9780134757599";
        var googleBooksJson = """
        {
            "items": [
                {
                    "volumeInfo": {
                        "title": "Refactoring",
                        "authors": ["Martin Fowler"],
                        "pageCount": 448
                    }
                }
            ]
        }
        """;

        var testHandler = new TestHttpMessageHandler();
        testHandler.SetResponse("openlibrary.org/search.json", HttpStatusCode.NotFound, "");
        testHandler.SetResponse("openlibrary.org/isbn/", HttpStatusCode.NotFound, "");
        testHandler.SetResponse("googleapis.com/books/v1/volumes", HttpStatusCode.OK, googleBooksJson);

        using var httpClient = new HttpClient(testHandler);
        var resolver = new BookMetadataResolver(httpClient, _memoryCache, NullLogger<BookMetadataResolver>.Instance);

        var result = await resolver.ResolveAsync(validIsbn);

        result.Status.Should().Be(ResolutionStatus.Success);
        result.Metadata.Should().NotBeNull();
        result.Metadata!.Title.Should().Be("Refactoring");
        result.Metadata.Authors.Should().Be("Martin Fowler");
        testHandler.RequestCount.Should().Be(3);
    }

    [Fact]
    public async Task ResolveAsync_WhenAllProvidersReturnNotFound_ShouldReturnNotFoundStatus()
    {
        const string validIsbn = "9780132350884";
        var testHandler = new TestHttpMessageHandler();
        testHandler.SetResponse("openlibrary.org/search.json", HttpStatusCode.OK, "{\"numFound\": 0, \"docs\": []}");
        testHandler.SetResponse("openlibrary.org/isbn/", HttpStatusCode.NotFound, "");
        testHandler.SetResponse("googleapis.com/books/v1/volumes", HttpStatusCode.OK, "{\"items\": []}");

        using var httpClient = new HttpClient(testHandler);
        var resolver = new BookMetadataResolver(httpClient, _memoryCache, NullLogger<BookMetadataResolver>.Instance);

        var result = await resolver.ResolveAsync(validIsbn);

        result.Status.Should().Be(ResolutionStatus.NotFound);
        result.Metadata.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WhenHttpRequestThrows_ShouldReturnNetworkErrorStatus()
    {
        const string validIsbn = "9780132350884";
        var testHandler = new TestHttpMessageHandler();
        testHandler.SimulateNetworkFailure = true;

        using var httpClient = new HttpClient(testHandler);
        var resolver = new BookMetadataResolver(httpClient, _memoryCache, NullLogger<BookMetadataResolver>.Instance);

        var result = await resolver.ResolveAsync(validIsbn);

        result.Status.Should().Be(ResolutionStatus.NetworkError);
        result.Metadata.Should().BeNull();
        result.ErrorMessage.Should().Contain("internet connection");
    }

    private class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, (HttpStatusCode status, string body)> _responses = new();
        public int RequestCount { get; private set; }
        public bool SimulateNetworkFailure { get; set; }

        public void SetResponse(string urlSubstring, HttpStatusCode status, string body)
        {
            _responses[urlSubstring] = (status, body);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;

            if (SimulateNetworkFailure)
            {
                throw new HttpRequestException("Name resolution failed or connection dropped.");
            }

            var requestUrl = request.RequestUri?.ToString() ?? "";
            foreach (var (substring, (status, body)) in _responses)
            {
                if (requestUrl.Contains(substring))
                {
                    return Task.FromResult(new HttpResponseMessage(status)
                    {
                        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
                    });
                }
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("", System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
