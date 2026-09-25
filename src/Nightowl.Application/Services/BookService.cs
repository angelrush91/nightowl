using Nightowl.Application.DTOs;
using Nightowl.Application.Interfaces;
using Nightowl.Domain.Entities;
using Nightowl.Domain.Enums;
using Nightowl.Domain.Repositories;
using Nightowl.Domain.ValueObjects;

namespace Nightowl.Application.Services;

public class BookService : IBookService
{
    private readonly IBookRepository _repository;
    private readonly IIsbnLookupService _isbnLookupService;

    public BookService(IBookRepository repository, IIsbnLookupService isbnLookupService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _isbnLookupService = isbnLookupService ?? throw new ArgumentNullException(nameof(isbnLookupService));
    }

    public async Task<IReadOnlyList<BookSummaryDto>> GetAllBooksAsync(
        string? searchTerm = null, 
        ReadingStatus? status = null, 
        CancellationToken cancellationToken = default)
    {
        var books = status.HasValue
            ? await _repository.GetByStatusAsync(status.Value, cancellationToken)
            : await _repository.GetAllAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            books = books.Where(b => 
                b.Title.ToLowerInvariant().Contains(term) ||
                b.Authors.ToLowerInvariant().Contains(term) ||
                b.Isbn.Value.Contains(term) ||
                (b.Isbn.Isbn10 != null && b.Isbn.Isbn10.Contains(term))
            ).ToList();
        }

        return books
            .OrderByDescending(b => b.DateAdded)
            .Select(MapToSummary)
            .ToList();
    }

    public async Task<BookDetailsDto?> GetBookByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var book = await _repository.GetByIdAsync(id, cancellationToken);
        return book is null ? null : MapToDetails(book);
    }

    public async Task<BookDetailsDto> AddBookAsync(CreateBookDto dto, CancellationToken cancellationToken = default)
    {
        var isbn = new Isbn(dto.Isbn);

        if (await _repository.ExistsWithIsbnAsync(isbn.Value, cancellationToken))
        {
            throw new InvalidOperationException($"A book with ISBN '{isbn.Value}' is already registered in your library.");
        }

        var book = new Book(
            title: dto.Title,
            authors: dto.Authors,
            isbn: isbn,
            pageCount: dto.PageCount,
            subtitle: dto.Subtitle,
            publisher: dto.Publisher,
            publishedDate: dto.PublishedDate,
            coverUrl: dto.CoverUrl,
            description: dto.Description,
            status: dto.Status,
            initialPage: dto.InitialPage
        );

        await _repository.AddAsync(book, cancellationToken);
        return MapToDetails(book);
    }

    public async Task<BookDetailsDto> UpdateProgressAsync(Guid id, UpdateProgressDto dto, CancellationToken cancellationToken = default)
    {
        var book = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Book with ID '{id}' was not found.");

        book.UpdateProgress(dto.CurrentPage, dto.Notes, dto.LogDate);
        await _repository.UpdateAsync(book, cancellationToken);

        return MapToDetails(book);
    }

    public async Task UpdateStatusAsync(Guid id, ReadingStatus status, CancellationToken cancellationToken = default)
    {
        var book = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Book with ID '{id}' was not found.");

        book.SetStatus(status);
        await _repository.UpdateAsync(book, cancellationToken);
    }

    public async Task UpdateRatingAndReviewAsync(Guid id, int? rating, string? review, CancellationToken cancellationToken = default)
    {
        var book = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Book with ID '{id}' was not found.");

        book.SetRatingAndReview(rating, review);
        await _repository.UpdateAsync(book, cancellationToken);
    }

    public async Task UpdateBookDetailsAsync(Guid id, UpdateBookDto dto, CancellationToken cancellationToken = default)
    {
        var book = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Book with ID '{id}' was not found.");

        book.UpdateMetadata(
            title: dto.Title,
            authors: dto.Authors,
            pageCount: dto.PageCount,
            subtitle: dto.Subtitle,
            publisher: dto.Publisher,
            publishedDate: dto.PublishedDate,
            coverUrl: dto.CoverUrl,
            description: dto.Description
        );

        await _repository.UpdateAsync(book, cancellationToken);
    }

    public async Task DeleteBookAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var book = await _repository.GetByIdAsync(id, cancellationToken);
        if (book is not null)
        {
            await _repository.DeleteAsync(book, cancellationToken);
        }
    }

    public async Task<ReadingStatsDto> GetReadingStatsAsync(CancellationToken cancellationToken = default)
    {
        var books = await _repository.GetAllAsync(cancellationToken);
        return ReadingStatsCalculator.Calculate(books);
    }

    public async Task<IsbnBookMetadataDto?> LookupIsbnAsync(string rawIsbn, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawIsbn))
            return null;

        return await _isbnLookupService.LookupByIsbnAsync(rawIsbn, cancellationToken);
    }

    private static BookSummaryDto MapToSummary(Book book) => new(
        Id: book.Id,
        Title: book.Title,
        Subtitle: book.Subtitle,
        Authors: book.Authors,
        Isbn: book.Isbn.Value,
        CoverUrl: book.CoverUrl,
        PageCount: book.PageCount,
        CurrentPage: book.Progress.CurrentPage,
        ProgressPercentage: book.Progress.Percentage,
        Status: book.Status,
        Rating: book.Rating
    );

    private static BookDetailsDto MapToDetails(Book book) => new(
        Id: book.Id,
        Title: book.Title,
        Subtitle: book.Subtitle,
        Authors: book.Authors,
        Isbn: book.Isbn.Value,
        Isbn10: book.Isbn.Isbn10,
        PageCount: book.PageCount,
        CurrentPage: book.Progress.CurrentPage,
        ProgressPercentage: book.Progress.Percentage,
        IsCompleted: book.Progress.IsCompleted,
        RemainingPages: book.Progress.RemainingPages,
        Status: book.Status,
        Publisher: book.Publisher,
        PublishedDate: book.PublishedDate,
        CoverUrl: book.CoverUrl,
        Description: book.Description,
        DateAdded: book.DateAdded,
        DateStarted: book.DateStarted,
        DateCompleted: book.DateCompleted,
        Rating: book.Rating,
        Review: book.Review,
        Sessions: book.ReadingSessions
            .OrderByDescending(s => s.SessionDate)
            .Select(s => new ReadingSessionDto(
                s.Id,
                s.SessionDate,
                s.StartPage,
                s.EndPage,
                s.PagesRead,
                s.Notes))
            .ToList()
    );
}
