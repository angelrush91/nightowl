using FluentAssertions;
using Nightowl.Domain.Exceptions;
using Nightowl.Domain.ValueObjects;
using Xunit;

namespace Nightowl.Tests.Domain;

public class ReadingProgressTests
{
    [Fact]
    public void ReadingProgress_ShouldCalculatePercentageCorrectly()
    {
        var progress = new ReadingProgress(50, 200);

        progress.Percentage.Should().Be(25.0);
        progress.RemainingPages.Should().Be(150);
        progress.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void ReadingProgress_ShouldClampCurrentPageToTotalPages()
    {
        var progress = new ReadingProgress(250, 200);

        progress.CurrentPage.Should().Be(200);
        progress.Percentage.Should().Be(100.0);
        progress.IsCompleted.Should().BeTrue();
        progress.RemainingPages.Should().Be(0);
    }

    [Fact]
    public void ReadingProgress_ShouldHandleZeroTotalPagesGracefully()
    {
        var progress = new ReadingProgress(10, 0);

        progress.Percentage.Should().Be(0.0);
        progress.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void ReadingProgress_ShouldThrow_WhenNegativePages()
    {
        var act1 = () => new ReadingProgress(-5, 100);
        var act2 = () => new ReadingProgress(10, -50);

        act1.Should().Throw<InvalidReadingProgressException>();
        act2.Should().Throw<InvalidReadingProgressException>();
    }

    [Fact]
    public void WithCurrentPage_ShouldReturnUpdatedInstance()
    {
        var original = new ReadingProgress(20, 100);
        var updated = original.WithCurrentPage(60);

        updated.CurrentPage.Should().Be(60);
        updated.TotalPages.Should().Be(100);
        updated.Percentage.Should().Be(60.0);
        original.CurrentPage.Should().Be(20); // immutable
    }
}
