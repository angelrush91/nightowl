namespace Nightowl.Domain.Entities;

public class ReadingSession
{
    public Guid Id { get; private set; }
    public Guid BookId { get; private set; }
    public DateTime SessionDate { get; private set; }
    public int StartPage { get; private set; }
    public int EndPage { get; private set; }
    public int PagesRead => Math.Max(0, EndPage - StartPage);
    public string? Notes { get; private set; }

    // Required by EF Core
    private ReadingSession()
    {
    }

    public ReadingSession(Guid bookId, int startPage, int endPage, DateTime sessionDate, string? notes = null)
    {
        Id = Guid.NewGuid();
        BookId = bookId;
        StartPage = Math.Max(0, startPage);
        EndPage = Math.Max(startPage, endPage);
        SessionDate = sessionDate;
        Notes = notes;
    }
}
