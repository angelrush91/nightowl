using Microsoft.Extensions.Logging;
using Nightowl.Application.DTOs;
using Nightowl.Application.Interfaces;

namespace Nightowl.Infrastructure.ExternalServices;

public class CompositeIsbnLookupService : IIsbnLookupService
{
    private readonly OpenLibraryIsbnService _openLibraryService;
    private readonly GoogleBooksFallbackIsbnService _fallbackService;
    private readonly ILogger<CompositeIsbnLookupService> _logger;

    public CompositeIsbnLookupService(
        OpenLibraryIsbnService openLibraryService,
        GoogleBooksFallbackIsbnService fallbackService,
        ILogger<CompositeIsbnLookupService> logger)
    {
        _openLibraryService = openLibraryService ?? throw new ArgumentNullException(nameof(openLibraryService));
        _fallbackService = fallbackService ?? throw new ArgumentNullException(nameof(fallbackService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IsbnBookMetadataDto?> LookupByIsbnAsync(string rawIsbn, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting OpenLibrary lookup for ISBN: {RawIsbn}", rawIsbn);
        var result = await _openLibraryService.LookupByIsbnAsync(rawIsbn, cancellationToken);
        if (result != null)
        {
            return result;
        }

        _logger.LogInformation("OpenLibrary returned no result. Trying Google Books fallback for ISBN: {RawIsbn}", rawIsbn);
        return await _fallbackService.LookupByIsbnAsync(rawIsbn, cancellationToken);
    }
}
