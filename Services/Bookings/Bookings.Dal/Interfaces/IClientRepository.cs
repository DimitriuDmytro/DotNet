namespace Bookings.Dal.Interfaces;

using Bookings.Domain.Models;

/// <summary>
/// Репозиторій клієнтів, реалізований виключно на чистому ADO.NET (SqlCommand, SqlParameter з SqlDbType, SqlDataReader).
/// СВІДОМО НЕ УСПАДКОВУЄ IGenericRepository<Client>, оскільки:
/// 1) IGenericRepository<T> розрахований на конвенційну реалізацію через мікро-ORM Dapper (BaseDapperRepository<T>),
///    тоді як ClientRepository демонструє низькорівневу роботу з драйвером БД вручну без ORM;
/// 2) Вибірка клієнтів вимагає обов'язкової пагінації GetAllAsync(pageNumber, pageSize) та пошуку за унікальним Email.
/// Детальніше обґрунтовано у docs/decisions.md (ADR-07).
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
