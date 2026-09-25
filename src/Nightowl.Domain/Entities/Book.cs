using Nightowl.Domain.Enums;
using Nightowl.Domain.Exceptions;
using Nightowl.Domain.ValueObjects;

namespace Nightowl.Domain.Entities;

public class Book
{
    private readonly List<ReadingSession> _readingSessions = new();

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Subtitle { get; private set; }
    public string Authors { get; private set; } = string.Empty;
    public Isbn Isbn { get; private set; } = null!;
    public int PageCount { get; private set; }
    public string? Publisher { get; private set; }
    public string? PublishedDate { get; private set; }
    public string? CoverUrl { get; private set; }
    public string? Description { get; private set; }
    public ReadingStatus Status { get; private set; }
    public ReadingProgress Progress { get; private set; } = null!;
    public DateTime DateAdded { get; private set; }
    public DateTime? DateStarted { get; private set; }
    public DateTime? DateCompleted { get; private set; }
    public int? Rating { get; private set; }
    public string? Review { get; private set; }

    public IReadOnlyCollection<ReadingSession> ReadingSessions => _readingSessions.AsReadOnly();

    // Required by EF Core
    private Book()
    {
    }

    public Book(
        string title,
        string authors,
        Isbn isbn,
        int pageCount,
        string? subtitle = null,
        string? publisher = null,
        string? publishedDate = null,
        string? coverUrl = null,
        string? description = null,
        ReadingStatus status = ReadingStatus.WantToRead,
        int initialPage = 0)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Book title cannot be empty.", nameof(title));

        Id = Guid.NewGuid();
        Title = title.Trim();
        Authors = string.IsNullOrWhiteSpace(authors) ? "Unknown Author" : authors.Trim();
        Isbn = isbn ?? throw new ArgumentNullException(nameof(isbn));
        PageCount = Math.Max(0, pageCount);
        Subtitle = subtitle?.Trim();
        Publisher = publisher?.Trim();
        PublishedDate = publishedDate?.Trim();
        CoverUrl = coverUrl?.Trim();
        Description = description?.Trim();
        Status = status;
        DateAdded = DateTime.UtcNow;

        Progress = new ReadingProgress(initialPage, PageCount);

        if (initialPage > 0)
        {
            DateStarted = DateTime.UtcNow;
            if (Progress.IsCompleted)
            {
                Status = ReadingStatus.Completed;
                DateCompleted = DateTime.UtcNow;
            }
            else if (Status == ReadingStatus.WantToRead)
            {
                Status = ReadingStatus.CurrentlyReading;
            }
        }
    }

    public void UpdateProgress(int newPage, string? notes = null, DateTime? logDate = null)
    {
        if (newPage < 0)
            throw new InvalidReadingProgressException($"Page cannot be negative ({newPage}).");

        int previousPage = Progress.CurrentPage;
        Progress = Progress.WithCurrentPage(newPage);

        if (newPage != previousPage)
        {
            var session = new ReadingSession(
                Id,
                previousPage,
                Progress.CurrentPage,
                logDate ?? DateTime.UtcNow,
                notes);
            _readingSessions.Add(session);
        }

        if (Progress.IsCompleted)
        {
            Status = ReadingStatus.Completed;
            DateCompleted ??= DateTime.UtcNow;
        }
        else if (Status == ReadingStatus.WantToRead && Progress.CurrentPage > 0)
        {
            Status = ReadingStatus.CurrentlyReading;
            DateStarted ??= DateTime.UtcNow;
        }
    }

    public void SetStatus(ReadingStatus newStatus)
    {
        var oldStatus = Status;
        Status = newStatus;

        if (newStatus == ReadingStatus.CurrentlyReading)
        {
            DateStarted ??= DateTime.UtcNow;
        }
        else if (newStatus == ReadingStatus.Completed)
        {
            DateCompleted ??= DateTime.UtcNow;
            DateStarted ??= DateTime.UtcNow;
            if (PageCount > 0 && Progress.CurrentPage < PageCount)
            {
                Progress = Progress.WithCurrentPage(PageCount);
            }
        }
        else if (oldStatus == ReadingStatus.Completed && newStatus != ReadingStatus.Completed)
        {
            DateCompleted = null;
        }
    }

    public void SetRatingAndReview(int? rating, string? review)
    {
        if (rating.HasValue && (rating.Value < 1 || rating.Value > 5))
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5 stars.");

        Rating = rating;
        Review = review?.Trim();
    }

    public void UpdateMetadata(
        string title,
        string authors,
        int pageCount,
        string? subtitle = null,
        string? publisher = null,
        string? publishedDate = null,
        string? coverUrl = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Book title cannot be empty.", nameof(title));

        Title = title.Trim();
        Authors = string.IsNullOrWhiteSpace(authors) ? "Unknown Author" : authors.Trim();
        PageCount = Math.Max(0, pageCount);
        Subtitle = subtitle?.Trim();
        Publisher = publisher?.Trim();
        PublishedDate = publishedDate?.Trim();
        CoverUrl = coverUrl?.Trim();
        Description = description?.Trim();

        Progress = Progress.WithTotalPages(PageCount);
    }
}
