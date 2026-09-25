using Nightowl.Application.DTOs;
using Nightowl.Domain.Enums;

namespace Nightowl.Application.Interfaces;

public interface IBookService
{
    Task<IReadOnlyList<BookSummaryDto>> GetAllBooksAsync(
        string? searchTerm = null, 
        ReadingStatus? status = null, 
        CancellationToken cancellationToken = default);

    Task<BookDetailsDto?> GetBookByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<BookDetailsDto> AddBookAsync(CreateBookDto dto, CancellationToken cancellationToken = default);

    Task<BookDetailsDto> UpdateProgressAsync(Guid id, UpdateProgressDto dto, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(Guid id, ReadingStatus status, CancellationToken cancellationToken = default);

    Task UpdateRatingAndReviewAsync(Guid id, int? rating, string? review, CancellationToken cancellationToken = default);

    Task UpdateBookDetailsAsync(Guid id, UpdateBookDto dto, CancellationToken cancellationToken = default);

    Task DeleteBookAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ReadingStatsDto> GetReadingStatsAsync(CancellationToken cancellationToken = default);

    Task<IsbnBookMetadataDto?> LookupIsbnAsync(string rawIsbn, CancellationToken cancellationToken = default);
}
