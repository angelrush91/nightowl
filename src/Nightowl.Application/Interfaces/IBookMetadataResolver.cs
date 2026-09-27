using Nightowl.Application.DTOs;

namespace Nightowl.Application.Interfaces;

public enum ResolutionStatus
{
    Success,
    NotFound,
    NetworkError,
    InvalidIsbn
}

public record BookMetadataResolution(
    ResolutionStatus Status,
    IsbnBookMetadataDto? Metadata = null,
    string? ErrorMessage = null
);

public interface IBookMetadataResolver
{
    Task<BookMetadataResolution> ResolveAsync(string rawIsbn, CancellationToken cancellationToken = default);
}
