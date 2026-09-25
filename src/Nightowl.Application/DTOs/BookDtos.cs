using Nightowl.Domain.Enums;

namespace Nightowl.Application.DTOs;

public record BookSummaryDto(
    Guid Id,
    string Title,
    string? Subtitle,
    string Authors,
    string Isbn,
    string? CoverUrl,
    int PageCount,
    int CurrentPage,
    double ProgressPercentage,
    ReadingStatus Status,
    int? Rating
);

public record ReadingSessionDto(
    Guid Id,
    DateTime SessionDate,
    int StartPage,
    int EndPage,
    int PagesRead,
    string? Notes
);

public record BookDetailsDto(
    Guid Id,
    string Title,
    string? Subtitle,
    string Authors,
    string Isbn,
    string? Isbn10,
    int PageCount,
    int CurrentPage,
    double ProgressPercentage,
    bool IsCompleted,
    int RemainingPages,
    ReadingStatus Status,
    string? Publisher,
    string? PublishedDate,
    string? CoverUrl,
    string? Description,
    DateTime DateAdded,
    DateTime? DateStarted,
    DateTime? DateCompleted,
    int? Rating,
    string? Review,
    IReadOnlyList<ReadingSessionDto> Sessions
);

public record CreateBookDto(
    string Title,
    string Authors,
    string Isbn,
    int PageCount,
    string? Subtitle = null,
    string? Publisher = null,
    string? PublishedDate = null,
    string? CoverUrl = null,
    string? Description = null,
    ReadingStatus Status = ReadingStatus.WantToRead,
    int InitialPage = 0
);

public record UpdateBookDto(
    string Title,
    string Authors,
    int PageCount,
    string? Subtitle = null,
    string? Publisher = null,
    string? PublishedDate = null,
    string? CoverUrl = null,
    string? Description = null
);

public record UpdateProgressDto(
    int CurrentPage,
    string? Notes = null,
    DateTime? LogDate = null
);

public record IsbnBookMetadataDto(
    string Title,
    string? Subtitle,
    string Authors,
    string Isbn,
    int PageCount,
    string? Publisher,
    string? PublishDate,
    string? CoverUrl,
    string? Description
);

public record ReadingStatsDto(
    int TotalBooks,
    int BooksCompleted,
    int BooksCurrentlyReading,
    int BooksWantToRead,
    int TotalPagesRead,
    double CompletionPercentage,
    double? AverageRating
);
