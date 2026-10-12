namespace Bookings.Dal.Interfaces;

/// <summary>
/// Узагальнений інтерфейс репозиторію для базових CRUD-операцій.
/// </summary>
/// <typeparam name="T">Тип сутності</typeparam>
/// <typeparam name="TKey">Тип первинного ключа</typeparam>
public interface IGenericRepository<T, TKey> where T : class
{
    /// <summary>
    /// Отримати сутність за ідентифікатором (з урахуванням IsDeleted = 0).
    /// </summary>
    Task<T?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати всі активні сутності.
    /// </summary>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Додати новий запис та повернути його ідентифікатор.
    /// </summary>
    Task<TKey> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Оновити існуючий запис.
    /// </summary>
    Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// М'яке видалення запису (встановлення IsDeleted = 1).
    /// </summary>
    Task<bool> DeleteAsync(TKey id, string? deletedBy = null, CancellationToken cancellationToken = default);
}
