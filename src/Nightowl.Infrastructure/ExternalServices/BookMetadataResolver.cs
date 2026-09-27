using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Nightowl.Application.DTOs;
using Nightowl.Application.Interfaces;
using Nightowl.Domain.ValueObjects;

namespace Nightowl.Infrastructure.ExternalServices;

public class BookMetadataResolver : IBookMetadataResolver
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BookMetadataResolver> _logger;

    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(30);

    public BookMetadataResolver(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<BookMetadataResolver> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BookMetadataResolution> ResolveAsync(string rawIsbn, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawIsbn) || !Isbn.TryCreate(rawIsbn, out var isbn) || isbn is null)
        {
            _logger.LogWarning("Invalid ISBN provided for resolution: {RawIsbn}", rawIsbn);
            return new BookMetadataResolution(ResolutionStatus.InvalidIsbn, null, "Invalid ISBN format.");
        }

        var cacheKey = $"isbn_meta_{isbn.Value}";
        if (_cache.TryGetValue(cacheKey, out IsbnBookMetadataDto? cachedMetadata) && cachedMetadata is not null)
        {
            _logger.LogInformation("Resolved ISBN metadata from cache for {Isbn}", isbn.Value);
            return new BookMetadataResolution(ResolutionStatus.Success, cachedMetadata);
        }

        try
        {
            // 1. OpenLibrary Search API
            var searchUrl = $"https://openlibrary.org/search.json?isbn={isbn.Value}&fields=title,subtitle,author_name,publisher,publish_date,publish_year,number_of_pages_median,cover_i";
            using var searchResponse = await _httpClient.GetAsync(searchUrl, cancellationToken);
            if (searchResponse.IsSuccessStatusCode)
            {
                var searchJson = await searchResponse.Content.ReadAsStringAsync(cancellationToken);
                var searchResult = ParseOpenLibrarySearchResult(searchJson, isbn.Value);
                if (searchResult != null)
                {
                    _cache.Set(cacheKey, searchResult, DefaultCacheDuration);
                    _logger.LogInformation("Resolved metadata via OpenLibrary Search API for {Isbn}", isbn.Value);
                    return new BookMetadataResolution(ResolutionStatus.Success, searchResult);
                }
            }

            // 2. OpenLibrary Edition API
            var editionUrl = $"https://openlibrary.org/isbn/{isbn.Value}.json";
            using var editionResponse = await _httpClient.GetAsync(editionUrl, cancellationToken);
            if (editionResponse.IsSuccessStatusCode)
            {
                var editionJson = await editionResponse.Content.ReadAsStringAsync(cancellationToken);
                var editionResult = ParseOpenLibraryEditionResult(editionJson, isbn.Value);
                if (editionResult != null)
                {
                    _cache.Set(cacheKey, editionResult, DefaultCacheDuration);
                    _logger.LogInformation("Resolved metadata via OpenLibrary Edition API for {Isbn}", isbn.Value);
                    return new BookMetadataResolution(ResolutionStatus.Success, editionResult);
                }
            }

            // 3. Google Books Volume API
            var googleUrl = $"https://www.googleapis.com/books/v1/volumes?q=isbn:{isbn.Value}";
            using var googleResponse = await _httpClient.GetAsync(googleUrl, cancellationToken);
            if (googleResponse.IsSuccessStatusCode)
            {
                var googleJson = await googleResponse.Content.ReadAsStringAsync(cancellationToken);
                var googleResult = ParseGoogleBooksResult(googleJson, isbn.Value);
                if (googleResult != null)
                {
                    _cache.Set(cacheKey, googleResult, DefaultCacheDuration);
                    _logger.LogInformation("Resolved metadata via Google Books API for {Isbn}", isbn.Value);
                    return new BookMetadataResolution(ResolutionStatus.Success, googleResult);
                }
            }

            _logger.LogInformation("No bibliographic records found across providers for ISBN {Isbn}", isbn.Value);
            return new BookMetadataResolution(ResolutionStatus.NotFound, null, $"No metadata found for ISBN {isbn.Value}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Network error occurred while resolving metadata for ISBN {Isbn}", isbn.Value);
            return new BookMetadataResolution(ResolutionStatus.NetworkError, null, "Could not reach book metadata services. Please check your internet connection.");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Lookup timed out for ISBN {Isbn}", isbn.Value);
            return new BookMetadataResolution(ResolutionStatus.NetworkError, null, "Metadata lookup timed out. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error resolving ISBN {Isbn}", isbn.Value);
            return new BookMetadataResolution(ResolutionStatus.NetworkError, null, "An error occurred while resolving book details.");
        }
    }

    public static IsbnBookMetadataDto? ParseOpenLibrarySearchResult(string json, string isbnValue)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("docs", out var docsProp) || docsProp.ValueKind != JsonValueKind.Array || docsProp.GetArrayLength() == 0)
            {
                return null;
            }

            var docElement = docsProp[0];

            var title = docElement.TryGetProperty("title", out var titleProp) && !string.IsNullOrWhiteSpace(titleProp.GetString())
                ? titleProp.GetString()!
                : "Unknown Title";

            string? subtitle = docElement.TryGetProperty("subtitle", out var subProp)
                ? subProp.GetString()
                : null;

            var authorsList = new List<string>();
            if (docElement.TryGetProperty("author_name", out var authorProp) && authorProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var a in authorProp.EnumerateArray())
                {
                    var name = a.GetString();
                    if (!string.IsNullOrWhiteSpace(name))
                        authorsList.Add(name.Trim());
                }
            }
            var authors = authorsList.Count > 0 ? string.Join(", ", authorsList) : "Unknown Author";

            int pageCount = 0;
            if (docElement.TryGetProperty("number_of_pages_median", out var pagesProp) && pagesProp.TryGetInt32(out var pages))
            {
                pageCount = pages;
            }

            string? publisher = null;
            if (docElement.TryGetProperty("publisher", out var pubProp) && pubProp.ValueKind == JsonValueKind.Array)
            {
                var pubs = pubProp.EnumerateArray()
                    .Select(p => p.GetString())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Take(2)
                    .ToList();
                if (pubs.Count > 0) publisher = string.Join(", ", pubs);
            }

            string? publishDate = null;
            if (docElement.TryGetProperty("publish_year", out var pyProp) && pyProp.ValueKind == JsonValueKind.Array)
            {
                var firstYear = pyProp.EnumerateArray().FirstOrDefault();
                if (firstYear.ValueKind == JsonValueKind.Number && firstYear.TryGetInt32(out var yr))
                {
                    publishDate = yr.ToString();
                }
            }
            else if (docElement.TryGetProperty("publish_date", out var pdProp) && pdProp.ValueKind == JsonValueKind.Array)
            {
                publishDate = pdProp.EnumerateArray().FirstOrDefault().GetString();
            }

            string coverUrl;
            if (docElement.TryGetProperty("cover_i", out var coverProp) && coverProp.TryGetInt64(out var coverId) && coverId > 0)
            {
                coverUrl = $"https://covers.openlibrary.org/b/id/{coverId}-M.jpg";
            }
            else
            {
                coverUrl = $"https://covers.openlibrary.org/b/isbn/{isbnValue}-M.jpg";
            }

            return new IsbnBookMetadataDto(
                Title: title,
                Subtitle: subtitle,
                Authors: authors,
                Isbn: isbnValue,
                PageCount: pageCount,
                Publisher: publisher,
                PublishDate: publishDate,
                CoverUrl: coverUrl,
                Description: null
            );
        }
        catch
        {
            return null;
        }
    }

    public static IsbnBookMetadataDto? ParseOpenLibraryEditionResult(string json, string isbnValue)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var title = root.TryGetProperty("title", out var titleProp) && !string.IsNullOrWhiteSpace(titleProp.GetString())
                ? titleProp.GetString()!
                : "Unknown Title";

            string? subtitle = root.TryGetProperty("subtitle", out var subProp)
                ? subProp.GetString()
                : null;

            int pageCount = 0;
            if (root.TryGetProperty("number_of_pages", out var pagesProp) && pagesProp.TryGetInt32(out var pages))
            {
                pageCount = pages;
            }

            string? publisher = null;
            if (root.TryGetProperty("publishers", out var pubProp) && pubProp.ValueKind == JsonValueKind.Array)
            {
                var pubs = pubProp.EnumerateArray()
                    .Select(p => p.ValueKind == JsonValueKind.String ? p.GetString() : p.TryGetProperty("name", out var n) ? n.GetString() : null)
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToList();
                if (pubs.Count > 0) publisher = string.Join(", ", pubs);
            }

            string? publishDate = root.TryGetProperty("publish_date", out var pdProp) ? pdProp.GetString() : null;

            string? description = null;
            if (root.TryGetProperty("description", out var descProp))
            {
                if (descProp.ValueKind == JsonValueKind.String)
                    description = descProp.GetString();
                else if (descProp.ValueKind == JsonValueKind.Object && descProp.TryGetProperty("value", out var valProp))
                    description = valProp.GetString();
            }

            var coverUrl = $"https://covers.openlibrary.org/b/isbn/{isbnValue}-M.jpg";

            return new IsbnBookMetadataDto(
                Title: title,
                Subtitle: subtitle,
                Authors: "Unknown Author",
                Isbn: isbnValue,
                PageCount: pageCount,
                Publisher: publisher,
                PublishDate: publishDate,
                CoverUrl: coverUrl,
                Description: description
            );
        }
        catch
        {
            return null;
        }
    }

    public static IsbnBookMetadataDto? ParseGoogleBooksResult(string json, string isbnValue)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("items", out var items) || items.GetArrayLength() == 0)
                return null;

            var volumeInfo = items[0].GetProperty("volumeInfo");
            var title = volumeInfo.TryGetProperty("title", out var tp) ? tp.GetString() ?? "Unknown" : "Unknown";
            var subtitle = volumeInfo.TryGetProperty("subtitle", out var sp) ? sp.GetString() : null;

            var authorsList = new List<string>();
            if (volumeInfo.TryGetProperty("authors", out var ap) && ap.ValueKind == JsonValueKind.Array)
            {
                foreach (var author in ap.EnumerateArray())
                {
                    if (!string.IsNullOrWhiteSpace(author.GetString()))
                        authorsList.Add(author.GetString()!);
                }
            }
            var authors = authorsList.Count > 0 ? string.Join(", ", authorsList) : "Unknown Author";

            int pageCount = 0;
            if (volumeInfo.TryGetProperty("pageCount", out var pp) && pp.TryGetInt32(out var pages))
            {
                pageCount = pages;
            }

            var publisher = volumeInfo.TryGetProperty("publisher", out var pubProp) ? pubProp.GetString() : null;
            var publishDate = volumeInfo.TryGetProperty("publishedDate", out var pdProp) ? pdProp.GetString() : null;
            var description = volumeInfo.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;

            string? coverUrl = null;
            if (volumeInfo.TryGetProperty("imageLinks", out var imgProp))
            {
                if (imgProp.TryGetProperty("thumbnail", out var thumbProp))
                {
                    coverUrl = thumbProp.GetString()?.Replace("http://", "https://");
                }
            }

            return new IsbnBookMetadataDto(
                Title: title,
                Subtitle: subtitle,
                Authors: authors,
                Isbn: isbnValue,
                PageCount: pageCount,
                Publisher: publisher,
                PublishDate: publishDate,
                CoverUrl: coverUrl,
                Description: description
            );
        }
        catch
        {
            return null;
        }
    }
}
