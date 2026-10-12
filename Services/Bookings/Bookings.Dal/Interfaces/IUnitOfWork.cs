namespace Bookings.Dal.Interfaces;

/// <summary>
/// Одиниця роботи (Unit of Work) для координації транзакцій між репозиторіями.
/// Гарантує виконання операцій над єдиним відкритим з'єднанням SqlConnection.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    IClientRepository Clients { get; }
    IAppointmentRepository Appointments { get; }
    IPaymentRepository Payments { get; }
    ICatalogServiceRepository CatalogServices { get; }

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
