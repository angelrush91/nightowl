using Nightowl.Application.DTOs;
using Nightowl.Domain.Entities;
using Nightowl.Domain.Enums;

namespace Nightowl.Application.Services;

public static class ReadingStatsCalculator
{
    public static ReadingStatsDto Calculate(IEnumerable<Book> books)
    {
        var bookList = books.ToList();
        if (bookList.Count == 0)
        {
            return new ReadingStatsDto(
                TotalBooks: 0,
                BooksCompleted: 0,
                BooksCurrentlyReading: 0,
                BooksWantToRead: 0,
                TotalPagesRead: 0,
                CompletionPercentage: 0,
                AverageRating: null
            );
        }

        int total = bookList.Count;
        int completed = bookList.Count(b => b.Status == ReadingStatus.Completed);
        int reading = bookList.Count(b => b.Status == ReadingStatus.CurrentlyReading);
        int wantToRead = bookList.Count(b => b.Status == ReadingStatus.WantToRead);
        int totalPages = bookList.Sum(b => b.Progress.CurrentPage);

        double completionPercentage = Math.Round((double)completed / total * 100.0, 1);

        var ratedBooks = bookList.Where(b => b.Rating.HasValue).ToList();
        double? avgRating = ratedBooks.Count > 0
            ? Math.Round(ratedBooks.Average(b => b.Rating!.Value), 2)
            : null;

        return new ReadingStatsDto(
            TotalBooks: total,
            BooksCompleted: completed,
            BooksCurrentlyReading: reading,
            BooksWantToRead: wantToRead,
            TotalPagesRead: totalPages,
            CompletionPercentage: completionPercentage,
            AverageRating: avgRating
        );
    }
}
