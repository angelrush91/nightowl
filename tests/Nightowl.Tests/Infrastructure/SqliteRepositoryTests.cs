using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Nightowl.Domain.Entities;
using Nightowl.Domain.Enums;
using Nightowl.Domain.ValueObjects;
using Nightowl.Infrastructure.Data;
using Xunit;

namespace Nightowl.Tests.Infrastructure;

public class SqliteRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly NightowlDbContext _context;
    private readonly BookRepository _repository;

    public SqliteRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<NightowlDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new NightowlDbContext(options);
        _context.Database.EnsureCreated();

        _repository = new BookRepository(_context);
    }

    [Fact]
    public async Task AddAndGetByIdAsync_ShouldPersistBookAndValueObjects()
    {
        var isbn = new Isbn("978-0-13-235088-4");
        var book = new Book(
            title: "Clean Code",
            authors: "Robert C. Martin",
            isbn: isbn,
            pageCount: 464,
            status: ReadingStatus.WantToRead
        );

        await _repository.AddAsync(book);

        var retrieved = await _repository.GetByIdAsync(book.Id);

        retrieved.Should().NotBeNull();
        retrieved!.Title.Should().Be("Clean Code");
        retrieved.Isbn.Value.Should().Be("9780132350884");
        retrieved.Progress.CurrentPage.Should().Be(0);
        retrieved.Progress.TotalPages.Should().Be(464);
        retrieved.Status.Should().Be(ReadingStatus.WantToRead);
    }

    [Fact]
    public async Task UpdateProgress_ShouldPersistProgressAndReadingSessions()
    {
        var book = new Book("Refactoring", "Martin Fowler", new Isbn("978-0-201-48567-7"), 400);
        await _repository.AddAsync(book);

        book.UpdateProgress(50, "Completed chapter 1");
        await _repository.UpdateAsync(book);

        // Fetch using a fresh query to verify persistence
        var retrieved = await _repository.GetByIdAsync(book.Id);

        retrieved.Should().NotBeNull();
        retrieved!.Progress.CurrentPage.Should().Be(50);
        retrieved.Progress.Percentage.Should().Be(12.5);
        retrieved.Status.Should().Be(ReadingStatus.CurrentlyReading);
        retrieved.ReadingSessions.Should().HaveCount(1);

        var session = retrieved.ReadingSessions.First();
        session.StartPage.Should().Be(0);
        session.EndPage.Should().Be(50);
        session.Notes.Should().Be("Completed chapter 1");
    }

    [Fact]
    public async Task ExistsWithIsbnAsync_ShouldReturnTrueForExistingBook()
    {
        var book = new Book("Test Book", "Author", new Isbn("9780132350884"), 100);
        await _repository.AddAsync(book);

        var exists = await _repository.ExistsWithIsbnAsync("978-0-13-235088-4");
        var notExists = await _repository.ExistsWithIsbnAsync("9780321125217");

        exists.Should().BeTrue();
        notExists.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
