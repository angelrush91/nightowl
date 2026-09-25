using FluentAssertions;
using Nightowl.Domain.Entities;
using Nightowl.Domain.Enums;
using Nightowl.Domain.Exceptions;
using Nightowl.Domain.ValueObjects;
using Xunit;

namespace Nightowl.Tests.Domain;

public class BookTests
{
    private static Book CreateSampleBook(int pageCount = 300, ReadingStatus status = ReadingStatus.WantToRead, int initialPage = 0)
    {
        return new Book(
            title: "Domain-Driven Design",
            authors: "Eric Evans",
            isbn: new Isbn("978-0-321-12521-7"),
            pageCount: pageCount,
            subtitle: "Tackling Complexity in the Heart of Software",
            publisher: "Addison-Wesley",
            publishedDate: "2003",
            coverUrl: "https://covers.openlibrary.org/b/isbn/9780321125217-M.jpg",
            status: status,
            initialPage: initialPage
        );
    }

    [Fact]
    public void NewBook_ShouldInitializeCorrectly()
    {
        var book = CreateSampleBook(350);

        book.Title.Should().Be("Domain-Driven Design");
        book.Authors.Should().Be("Eric Evans");
        book.PageCount.Should().Be(350);
        book.Progress.CurrentPage.Should().Be(0);
        book.Status.Should().Be(ReadingStatus.WantToRead);
        book.DateStarted.Should().BeNull();
        book.DateCompleted.Should().BeNull();
        book.ReadingSessions.Should().BeEmpty();
    }

    [Fact]
    public void UpdateProgress_ShouldRecordSessionAndChangeStatusToReading()
    {
        var book = CreateSampleBook(300);

        book.UpdateProgress(50, "Read chapters 1 and 2");

        book.Progress.CurrentPage.Should().Be(50);
        book.Status.Should().Be(ReadingStatus.CurrentlyReading);
        book.DateStarted.Should().NotBeNull();
        book.DateCompleted.Should().BeNull();
        book.ReadingSessions.Should().HaveCount(1);

        var session = book.ReadingSessions.First();
        session.StartPage.Should().Be(0);
        session.EndPage.Should().Be(50);
        session.PagesRead.Should().Be(50);
        session.Notes.Should().Be("Read chapters 1 and 2");
    }

    [Fact]
    public void UpdateProgress_ToCompletion_ShouldSetCompletedStatusAndDate()
    {
        var book = CreateSampleBook(300);

        book.UpdateProgress(300, "Finished the entire book!");

        book.Progress.CurrentPage.Should().Be(300);
        book.Progress.IsCompleted.Should().BeTrue();
        book.Status.Should().Be(ReadingStatus.Completed);
        book.DateCompleted.Should().NotBeNull();
    }

    [Fact]
    public void SetStatus_ToCompleted_ShouldAutomaticallyMaxOutProgress()
    {
        var book = CreateSampleBook(300);

        book.SetStatus(ReadingStatus.Completed);

        book.Status.Should().Be(ReadingStatus.Completed);
        book.Progress.CurrentPage.Should().Be(300);
        book.DateCompleted.Should().NotBeNull();
    }

    [Fact]
    public void UpdateProgress_ShouldThrow_WhenNegativePage()
    {
        var book = CreateSampleBook(300);

        var act = () => book.UpdateProgress(-10);

        act.Should().Throw<InvalidReadingProgressException>();
    }

    [Fact]
    public void SetRatingAndReview_ShouldStoreValidValues()
    {
        var book = CreateSampleBook(300);

        book.SetRatingAndReview(5, "A foundational masterpiece.");

        book.Rating.Should().Be(5);
        book.Review.Should().Be("A foundational masterpiece.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void SetRatingAndReview_ShouldThrow_WhenRatingOutOfRange(int invalidRating)
    {
        var book = CreateSampleBook(300);

        var act = () => book.SetRatingAndReview(invalidRating, "Nice");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
