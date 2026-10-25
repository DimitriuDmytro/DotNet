namespace Catalog.Dal.Interfaces;

using Ardalis.Specification;

/// <summary>
/// Узагальнений асинхронний репозиторій на базі DbSet&lt;T&gt; (Критерій 5).
/// Не зберігає зміни самостійно — операції фіксуються через IUnitOfWork.SaveChangesAsync.
/// </summary>
public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Delete(T entity);

    // Підтримка патерну Специфікація (Критерій 14)
    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
    Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
}
