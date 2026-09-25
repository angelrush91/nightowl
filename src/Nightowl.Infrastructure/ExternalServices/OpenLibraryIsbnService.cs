using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nightowl.Application.DTOs;
using Nightowl.Application.Interfaces;
using Nightowl.Domain.ValueObjects;

namespace Nightowl.Infrastructure.ExternalServices;

public class OpenLibraryIsbnService : IIsbnLookupService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenLibraryIsbnService> _logger;

    public OpenLibraryIsbnService(HttpClient httpClient, ILogger<OpenLibraryIsbnService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IsbnBookMetadataDto?> LookupByIsbnAsync(string rawIsbn, CancellationToken cancellationToken = default)
    {
        if (!Isbn.TryCreate(rawIsbn, out var isbn) || isbn is null)
        {
            _logger.LogWarning("Invalid ISBN format provided for lookup: {RawIsbn}", rawIsbn);
            return null;
        }

        try
        {
            // 1. Try OpenLibrary Search API (returns title, authors, cover_i, pages, etc.)
            var searchUrl = $"https://openlibrary.org/search.json?isbn={isbn.Value}&fields=title,subtitle,author_name,publisher,publish_date,publish_year,number_of_pages_median,cover_i";
            using var searchResponse = await _httpClient.GetAsync(searchUrl, cancellationToken);
            if (searchResponse.IsSuccessStatusCode)
            {
                var searchJson = await searchResponse.Content.ReadAsStringAsync(cancellationToken);
                var searchResult = ParseSearchResult(searchJson, isbn.Value);
                if (searchResult != null)
                {
                    _logger.LogInformation("Successfully resolved book metadata via OpenLibrary Search API for ISBN {Isbn}", isbn.Value);
                    return searchResult;
                }
            }

            // 2. Fallback to OpenLibrary Direct Edition API
            var editionUrl = $"https://openlibrary.org/isbn/{isbn.Value}.json";
            using var editionResponse = await _httpClient.GetAsync(editionUrl, cancellationToken);
            if (editionResponse.IsSuccessStatusCode)
            {
                var editionJson = await editionResponse.Content.ReadAsStringAsync(cancellationToken);
                var editionResult = ParseEditionResult(editionJson, isbn.Value);
                if (editionResult != null)
                {
                    _logger.LogInformation("Successfully resolved book metadata via OpenLibrary Edition API for ISBN {Isbn}", isbn.Value);
                    return editionResult;
                }
            }

            _logger.LogInformation("No OpenLibrary records found for ISBN {Isbn}", isbn.Value);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query OpenLibrary for ISBN {Isbn}", isbn.Value);
            return null;
        }
    }

    public static IsbnBookMetadataDto? ParseSearchResult(string json, string isbnValue)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

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

    public static IsbnBookMetadataDto? ParseEditionResult(string json, string isbnValue)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

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
}
