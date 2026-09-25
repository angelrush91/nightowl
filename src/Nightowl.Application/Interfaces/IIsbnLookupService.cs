using Nightowl.Application.DTOs;

namespace Nightowl.Application.Interfaces;

public interface IIsbnLookupService
{
    Task<IsbnBookMetadataDto?> LookupByIsbnAsync(string rawIsbn, CancellationToken cancellationToken = default);
}
