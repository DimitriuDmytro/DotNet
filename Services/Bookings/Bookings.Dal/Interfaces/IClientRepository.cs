namespace Bookings.Dal.Interfaces;

using Bookings.Domain.Models;

/// <summary>
/// Репозиторій клієнтів (реалізований виключно на чистому ADO.NET без Dapper).
/// Не успадковує IGenericRepository для демонстрації архітектурної різниці.
/// </summary>
public interface IClientRepository
{
    Task<Client?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Client?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Client>> GetAllAsync(int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<long> AddAsync(Client client, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Client client, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, string? deletedBy = null, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
}
