using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nightowl.Application.DTOs;
using Nightowl.Application.Interfaces;
using Nightowl.Domain.ValueObjects;

namespace Nightowl.Infrastructure.ExternalServices;

public class GoogleBooksFallbackIsbnService : IIsbnLookupService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleBooksFallbackIsbnService> _logger;

    public GoogleBooksFallbackIsbnService(HttpClient httpClient, ILogger<GoogleBooksFallbackIsbnService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IsbnBookMetadataDto?> LookupByIsbnAsync(string rawIsbn, CancellationToken cancellationToken = default)
    {
        if (!Isbn.TryCreate(rawIsbn, out var isbn) || isbn is null)
            return null;

        try
        {
            var url = $"https://www.googleapis.com/books/v1/volumes?q=isbn:{isbn.Value}";
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
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
                Isbn: isbn.Value,
                PageCount: pageCount,
                Publisher: publisher,
                PublishDate: publishDate,
                CoverUrl: coverUrl,
                Description: description
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Google Books fallback failed for ISBN {Isbn}", isbn.Value);
            return null;
        }
    }
}
