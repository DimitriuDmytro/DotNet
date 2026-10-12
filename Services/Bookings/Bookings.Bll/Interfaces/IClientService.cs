namespace Bookings.Bll.Interfaces;

using Bookings.Bll.DTOs;

public interface IClientService
{
    Task<ClientDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ClientDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<PagedResult<ClientDto>> GetAllAsync(int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<ClientDto> CreateAsync(CreateClientDto dto, CancellationToken cancellationToken = default);
    Task<ClientDto> UpdateAsync(long id, UpdateClientDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, string? deletedBy = null, CancellationToken cancellationToken = default);
}
