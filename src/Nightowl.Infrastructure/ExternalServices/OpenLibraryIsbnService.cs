using System.Text.Json;
using System.Text.Json.Serialization;
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
            // OpenLibrary books data API
            // Returns: { "ISBN:9780132350884": { title: "Clean Code", authors: [...], ... } }
            var key = $"ISBN:{isbn.Value}";
            var url = $"https://openlibrary.org/api/books?bibkeys={key}&jscmd=data&format=json";

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenLibrary API returned status {StatusCode} for ISBN {Isbn}", response.StatusCode, isbn.Value);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}")
            {
                _logger.LogInformation("No OpenLibrary data found for ISBN {Isbn}", isbn.Value);
                return null;
            }

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty(key, out var bookElement))
            {
                // Try with ISBN-10 if available
                if (isbn.Isbn10 != null && doc.RootElement.TryGetProperty($"ISBN:{isbn.Isbn10}", out var altElement))
                {
                    bookElement = altElement;
                }
                else
                {
                    return null;
                }
            }

            return ParseOpenLibraryElement(bookElement, isbn.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query OpenLibrary for ISBN {Isbn}", isbn.Value);
            return null;
        }
    }

    public static IsbnBookMetadataDto ParseOpenLibraryElement(JsonElement bookElement, string isbnValue)
    {
        var title = bookElement.TryGetProperty("title", out var titleProp) 
            ? titleProp.GetString() ?? "Unknown Title" 
            : "Unknown Title";

        string? subtitle = bookElement.TryGetProperty("subtitle", out var subProp) 
            ? subProp.GetString() 
            : null;

        var authorsList = new List<string>();
        if (bookElement.TryGetProperty("authors", out var authorsProp) && authorsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var author in authorsProp.EnumerateArray())
            {
                if (author.TryGetProperty("name", out var nameProp) && !string.IsNullOrWhiteSpace(nameProp.GetString()))
                {
                    authorsList.Add(nameProp.GetString()!.Trim());
                }
            }
        }
        var authors = authorsList.Count > 0 ? string.Join(", ", authorsList) : "Unknown Author";

        int pageCount = 0;
        if (bookElement.TryGetProperty("number_of_pages", out var pagesProp) && pagesProp.TryGetInt32(out var pages))
        {
            pageCount = pages;
        }

        string? publisher = null;
        if (bookElement.TryGetProperty("publishers", out var pubProp) && pubProp.ValueKind == JsonValueKind.Array)
        {
            var pubs = pubProp.EnumerateArray()
                .Select(p => p.TryGetProperty("name", out var n) ? n.GetString() : null)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();
            if (pubs.Count > 0) publisher = string.Join(", ", pubs);
        }

        string? publishDate = bookElement.TryGetProperty("publish_date", out var dateProp) 
            ? dateProp.GetString() 
            : null;

        string? coverUrl = null;
        if (bookElement.TryGetProperty("cover", out var coverProp) && coverProp.ValueKind == JsonValueKind.Object)
        {
            if (coverProp.TryGetProperty("large", out var largeUrl) && !string.IsNullOrWhiteSpace(largeUrl.GetString()))
            {
                coverUrl = largeUrl.GetString();
            }
            else if (coverProp.TryGetProperty("medium", out var medUrl) && !string.IsNullOrWhiteSpace(medUrl.GetString()))
            {
                coverUrl = medUrl.GetString();
            }
            else if (coverProp.TryGetProperty("small", out var smallUrl) && !string.IsNullOrWhiteSpace(smallUrl.GetString()))
            {
                coverUrl = smallUrl.GetString();
            }
        }

        // Fallback standard OpenLibrary cover URL if not in payload
        coverUrl ??= $"https://covers.openlibrary.org/b/isbn/{isbnValue}-M.jpg";

        string? description = null;
        if (bookElement.TryGetProperty("description", out var descProp))
        {
            if (descProp.ValueKind == JsonValueKind.String)
            {
                description = descProp.GetString();
            }
            else if (descProp.ValueKind == JsonValueKind.Object && descProp.TryGetProperty("value", out var valProp))
            {
                description = valProp.GetString();
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
}
