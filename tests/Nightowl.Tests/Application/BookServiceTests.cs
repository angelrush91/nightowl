using FluentAssertions;
using Moq;
using Nightowl.Application.DTOs;
using Nightowl.Application.Interfaces;
using Nightowl.Application.Services;
using Nightowl.Domain.Entities;
using Nightowl.Domain.Enums;
using Nightowl.Domain.Repositories;
using Nightowl.Domain.ValueObjects;
using Xunit;

namespace Nightowl.Tests.Application;

public class BookServiceTests
{
    private readonly Mock<IBookRepository> _mockRepo;
    private readonly Mock<IIsbnLookupService> _mockLookup;
    private readonly BookService _service;

    public BookServiceTests()
    {
        _mockRepo = new Mock<IBookRepository>();
        _mockLookup = new Mock<IIsbnLookupService>();
        _service = new BookService(_mockRepo.Object, _mockLookup.Object);
    }

    [Fact]
    public async Task AddBookAsync_ShouldSaveBook_WhenValid()
    {
        var dto = new CreateBookDto(
            Title: "Clean Code",
            Authors: "Robert C. Martin",
            Isbn: "978-0-13-235088-4",
            PageCount: 464,
            Publisher: "Prentice Hall"
        );

        _mockRepo.Setup(r => r.ExistsWithIsbnAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.AddBookAsync(dto);

        result.Should().NotBeNull();
        result.Title.Should().Be("Clean Code");
        result.Isbn.Should().Be("9780132350884");
        result.PageCount.Should().Be(464);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddBookAsync_ShouldThrow_WhenBookAlreadyExists()
    {
        var dto = new CreateBookDto(
            Title: "Clean Code",
            Authors: "Robert C. Martin",
            Isbn: "978-0-13-235088-4",
            PageCount: 464
        );

        _mockRepo.Setup(r => r.ExistsWithIsbnAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = async () => await _service.AddBookAsync(dto);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public async Task UpdateProgressAsync_ShouldUpdatePageAndReturnDetails()
    {
        var book = new Book("Clean Code", "Robert C. Martin", new Isbn("978-0-13-235088-4"), 464);
        _mockRepo.Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        var updateDto = new UpdateProgressDto(100, "Read first few chapters");
        var result = await _service.UpdateProgressAsync(book.Id, updateDto);

        result.CurrentPage.Should().Be(100);
        result.Status.Should().Be(ReadingStatus.CurrentlyReading);
        result.Sessions.Should().HaveCount(1);
        _mockRepo.Verify(r => r.UpdateAsync(book, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetReadingStatsAsync_ShouldCalculateCorrectMetrics()
    {
        var book1 = new Book("Book 1", "Author 1", new Isbn("978-0-13-235088-4"), 200);
        book1.UpdateProgress(200); // completed

        var book2 = new Book("Book 2", "Author 2", new Isbn("978-0-321-12521-7"), 400);
        book2.UpdateProgress(100); // currently reading

        var book3 = new Book("Book 3", "Author 3", new Isbn("0-306-40615-2"), 150); // want to read

        _mockRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Book> { book1, book2, book3 });

        var stats = await _service.GetReadingStatsAsync();

        stats.TotalBooks.Should().Be(3);
        stats.BooksCompleted.Should().Be(1);
        stats.BooksCurrentlyReading.Should().Be(1);
        stats.BooksWantToRead.Should().Be(1);
        stats.TotalPagesRead.Should().Be(300); // 200 + 100
        stats.CompletionPercentage.Should().Be(33.3);
    }

    [Fact]
    public async Task GetAllBooksAsync_ShouldFilterBySearchTerm()
    {
        var book1 = new Book("Refactoring", "Martin Fowler", new Isbn("978-0-13-235088-4"), 300);
        var book2 = new Book("Design Patterns", "Gang of Four", new Isbn("978-0-321-12521-7"), 400);

        _mockRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Book> { book1, book2 });

        var result = await _service.GetAllBooksAsync(searchTerm: "Fowler");

        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Refactoring");
    }
}
