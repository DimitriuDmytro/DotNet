namespace Bookings.Dal.Interfaces;

/// <summary>
/// Узагальнений інтерфейс репозиторію для базових CRUD-операцій над доменними сутностями.
/// Конкретні Dapper-інтерфейси (IAppointmentRepository, IPaymentRepository, ICatalogServiceRepository)
/// успадковують цей контракт і додають виключно специфічні запити та процедури.
/// </summary>
/// <typeparam name="T">Тип доменної сутності</typeparam>
public interface IGenericRepository<T> where T : class
{
    /// <summary>
    /// Отримати сутність за ідентифікатором (з урахуванням IsDeleted = 0).
    /// </summary>
    Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати всі активні сутності.
    /// </summary>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Додати новий запис та повернути його ідентифікатор.
    /// </summary>
    Task<long> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Оновити існуючий запис.
    /// </summary>
    Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// М'яке видалення запису (встановлення IsDeleted = 1).
    /// </summary>
    Task<bool> DeleteAsync(long id, string? deletedBy = null, CancellationToken cancellationToken = default);
}
