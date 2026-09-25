using FluentAssertions;
using Nightowl.Domain.Exceptions;
using Nightowl.Domain.ValueObjects;
using Xunit;

namespace Nightowl.Tests.Domain;

public class IsbnTests
{
    [Theory]
    [InlineData("0-306-40615-2")]
    [InlineData("0306406152")]
    [InlineData("0471958697")]
    [InlineData("0-13-235088-2")] // Clean Code (ISBN-10)
    public void Isbn_ShouldAccept_ValidIsbn10(string raw)
    {
        var isbn = new Isbn(raw);

        isbn.Isbn10.Should().NotBeNull();
        isbn.Isbn13.Should().StartWith("978");
        isbn.Value.Should().Be(isbn.Isbn13);
    }

    [Theory]
    [InlineData("978-0-13-235088-4")] // Clean Code (ISBN-13)
    [InlineData("9780132350884")]
    [InlineData("978-0-321-12521-7")] // DDD by Eric Evans
    [InlineData("9780321125217")]
    public void Isbn_ShouldAccept_ValidIsbn13(string raw)
    {
        var isbn = new Isbn(raw);

        isbn.Isbn13.Should().Be("9780132350884".Length == raw.Replace("-", "").Length ? raw.Replace("-", "") : isbn.Isbn13);
        isbn.Value.Should().HaveLength(13);
    }

    [Theory]
    [InlineData("0-8044-2957-X")] // Valid ISBN-10 with check digit X
    public void Isbn_ShouldAccept_ValidIsbn10WithCheckDigitX(string raw)
    {
        var isbn = new Isbn(raw);

        isbn.Isbn10.Should().EndWith("X");
        isbn.Isbn13.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]
    [InlineData("0-13-235088-9")] // invalid check digit
    [InlineData("978-0-13-235088-9")] // invalid check digit
    [InlineData("abcdefghij")]
    public void Isbn_ShouldThrow_WhenInvalid(string raw)
    {
        var act = () => new Isbn(raw);

        act.Should().Throw<InvalidIsbnException>();
    }

    [Fact]
    public void TryCreate_ShouldReturnFalse_WhenInvalid()
    {
        bool result = Isbn.TryCreate("invalid-isbn", out var isbn);

        result.Should().BeFalse();
        isbn.Should().BeNull();
    }

    [Fact]
    public void TryCreate_ShouldReturnTrue_WhenValid()
    {
        bool result = Isbn.TryCreate("9780132350884", out var isbn);

        result.Should().BeTrue();
        isbn.Should().NotBeNull();
        isbn!.Value.Should().Be("9780132350884");
    }

    [Fact]
    public void ConvertIsbn10To13_ShouldCalculateCorrectCheckDigit()
    {
        // Clean Code ISBN-10 is 0132350882 -> ISBN-13 is 9780132350884
        var isbn = new Isbn("0132350882");

        isbn.Isbn13.Should().Be("9780132350884");
    }
}
