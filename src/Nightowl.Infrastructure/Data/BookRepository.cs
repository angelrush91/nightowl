using Microsoft.EntityFrameworkCore;
using Nightowl.Domain.Entities;
using Nightowl.Domain.Enums;
using Nightowl.Domain.Repositories;
using Nightowl.Domain.ValueObjects;

namespace Nightowl.Infrastructure.Data;

public class BookRepository : IBookRepository
{
    private readonly NightowlDbContext _context;

    public BookRepository(NightowlDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Books
            .Include(b => b.ReadingSessions)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<Book?> GetByIsbnAsync(string isbnValue, CancellationToken cancellationToken = default)
    {
        var isbn = new Isbn(isbnValue);
        return await _context.Books
            .Include(b => b.ReadingSessions)
            .FirstOrDefaultAsync(b => b.Isbn == isbn, cancellationToken);
    }

    public async Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Books
            .Include(b => b.ReadingSessions)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Book>> GetByStatusAsync(ReadingStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Books
            .Include(b => b.ReadingSessions)
            .Where(b => b.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsWithIsbnAsync(string isbnValue, CancellationToken cancellationToken = default)
    {
        var isbn = new Isbn(isbnValue);
        return await _context.Books
            .AnyAsync(b => b.Isbn == isbn, cancellationToken);
    }

    public async Task AddAsync(Book book, CancellationToken cancellationToken = default)
    {
        await _context.Books.AddAsync(book, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Book book, CancellationToken cancellationToken = default)
    {
        var entry = _context.Entry(book);
        if (entry.State == EntityState.Detached)
        {
            _context.Books.Attach(book);
            entry.State = EntityState.Modified;
        }

        foreach (var session in book.ReadingSessions)
        {
            var sessionEntry = _context.Entry(session);
            if (sessionEntry.State != EntityState.Added)
            {
                var existsInDb = await _context.ReadingSessions.AsNoTracking().AnyAsync(s => s.Id == session.Id, cancellationToken);
                sessionEntry.State = existsInDb ? EntityState.Modified : EntityState.Added;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Book book, CancellationToken cancellationToken = default)
    {
        if (_context.Entry(book).State == EntityState.Detached)
        {
            _context.Books.Attach(book);
        }
        _context.Books.Remove(book);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
