using Nightowl.Domain.Exceptions;

namespace Nightowl.Domain.ValueObjects;

public sealed record ReadingProgress
{
    public int CurrentPage { get; }
    public int TotalPages { get; }

    public ReadingProgress(int currentPage, int totalPages)
    {
        if (currentPage < 0)
        {
            throw new InvalidReadingProgressException($"Current page cannot be negative. Got {currentPage}.");
        }

        if (totalPages < 0)
        {
            throw new InvalidReadingProgressException($"Total pages cannot be negative. Got {totalPages}.");
        }

        CurrentPage = totalPages > 0 ? Math.Min(currentPage, totalPages) : currentPage;
        TotalPages = totalPages;
    }

    public static ReadingProgress Initial(int totalPages) => new(0, totalPages);

    public double Percentage =>
        TotalPages > 0 ? Math.Min(100.0, Math.Round((double)CurrentPage / TotalPages * 100.0, 1)) : 0.0;

    public bool IsCompleted => TotalPages > 0 && CurrentPage >= TotalPages;

    public int RemainingPages => Math.Max(0, TotalPages - CurrentPage);

    public ReadingProgress WithCurrentPage(int newCurrentPage) => new(newCurrentPage, TotalPages);

    public ReadingProgress WithTotalPages(int newTotalPages) => new(CurrentPage, newTotalPages);

    public override string ToString() => TotalPages > 0 
        ? $"{CurrentPage} / {TotalPages} ({Percentage:F1}%)" 
        : $"Page {CurrentPage}";
}
