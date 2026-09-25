using Nightowl.Domain.Entities;
using Nightowl.Domain.Enums;

namespace Nightowl.Domain.Repositories;

public interface IBookRepository
{
    Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Book?> GetByIsbnAsync(string isbnValue, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Book>> GetByStatusAsync(ReadingStatus status, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithIsbnAsync(string isbnValue, CancellationToken cancellationToken = default);
    Task AddAsync(Book book, CancellationToken cancellationToken = default);
    Task UpdateAsync(Book book, CancellationToken cancellationToken = default);
    Task DeleteAsync(Book book, CancellationToken cancellationToken = default);
}
