namespace Bookings.Dal.Repositories;

using System.Data;
using Microsoft.Data.SqlClient;
using Bookings.Dal.Common;
using Bookings.Dal.Interfaces;
using Bookings.Domain.Models;

/// <summary>
/// Репозиторій клієнтів, реалізований ВИКЛЮЧНО на чистому ADO.NET (без використання Dapper).
/// Демонструє явне керування командами SqlCommand, параметрами SqlParameter з типізацією SqlDbType,
/// асинхронним звільненням ресурсів (await using) та постовпчиковим вичитуванням SqlDataReader.
/// Свідомо НЕ успадковує IGenericRepository<T>.
/// </summary>
public class ClientRepository : RepositoryBase, IClientRepository
{
    public ClientRepository(SqlConnection connection, Func<SqlTransaction?> transactionProvider)
        : base(connection, transactionProvider)
    {
    }

    public async Task<Client?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            const string sql = @"
                SELECT Id, Email, FullName, Phone, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted
                FROM dbo.Clients
                WHERE Id = @Id AND IsDeleted = 0;";

            await using var command = new SqlCommand(sql, Connection, Transaction)
            {
                CommandTimeout = DefaultCommandTimeoutSeconds
            };
            command.Parameters.Add(new SqlParameter("@Id", SqlDbType.BigInt) { Value = id });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return MapClient(reader);
            }

            return null;
        });
    }

    public async Task<Client?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            const string sql = @"
                SELECT Id, Email, FullName, Phone, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted
                FROM dbo.Clients
                WHERE Email = @Email AND IsDeleted = 0;";

            await using var command = new SqlCommand(sql, Connection, Transaction)
            {
                CommandTimeout = DefaultCommandTimeoutSeconds
            };
            command.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 255) { Value = email });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return MapClient(reader);
            }

            return null;
        });
    }

    public async Task<IReadOnlyList<Client>> GetAllAsync(int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            var offset = (pageNumber - 1) * pageSize;

            const string sql = @"
                SELECT Id, Email, FullName, Phone, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted
                FROM dbo.Clients
                WHERE IsDeleted = 0
                ORDER BY Id DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            await using var command = new SqlCommand(sql, Connection, Transaction)
            {
                CommandTimeout = DefaultCommandTimeoutSeconds
            };
            command.Parameters.Add(new SqlParameter("@Offset", SqlDbType.Int) { Value = offset });
            command.Parameters.Add(new SqlParameter("@PageSize", SqlDbType.Int) { Value = pageSize });

            var list = new List<Client>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(MapClient(reader));
            }

            return list;
        });
    }

    public async Task<long> AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            const string sql = @"
                INSERT INTO dbo.Clients (Email, FullName, Phone, CreatedAt, CreatedBy, IsDeleted)
                VALUES (@Email, @FullName, @Phone, SYSUTCDATETIME(), @CreatedBy, 0);
                SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

            await using var command = new SqlCommand(sql, Connection, Transaction)
            {
                CommandTimeout = DefaultCommandTimeoutSeconds
            };
            command.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 255) { Value = client.Email });
            command.Parameters.Add(new SqlParameter("@FullName", SqlDbType.NVarChar, 200) { Value = client.FullName });
            command.Parameters.Add(new SqlParameter("@Phone", SqlDbType.NVarChar, 30) { Value = client.Phone });
            command.Parameters.Add(new SqlParameter("@CreatedBy", SqlDbType.NVarChar, 100) { Value = client.CreatedBy ?? "system" });

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(result);
        });
    }

    public async Task<bool> UpdateAsync(Client client, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            const string sql = @"
                UPDATE dbo.Clients
                SET Email = @Email,
                    FullName = @FullName,
                    Phone = @Phone,
                    UpdatedAt = SYSUTCDATETIME(),
                    UpdatedBy = @UpdatedBy
                WHERE Id = @Id AND IsDeleted = 0;";

            await using var command = new SqlCommand(sql, Connection, Transaction)
            {
                CommandTimeout = DefaultCommandTimeoutSeconds
            };
            command.Parameters.Add(new SqlParameter("@Id", SqlDbType.BigInt) { Value = client.Id });
            command.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 255) { Value = client.Email });
            command.Parameters.Add(new SqlParameter("@FullName", SqlDbType.NVarChar, 200) { Value = client.FullName });
            command.Parameters.Add(new SqlParameter("@Phone", SqlDbType.NVarChar, 30) { Value = client.Phone });
            command.Parameters.Add(new SqlParameter("@UpdatedBy", SqlDbType.NVarChar, 100) { Value = (object?)client.UpdatedBy ?? DBNull.Value });

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            return rowsAffected > 0;
        });
    }

    public async Task<bool> DeleteAsync(long id, string? deletedBy = null, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            const string sql = @"
                UPDATE dbo.Clients
                SET IsDeleted = 1,
                    UpdatedAt = SYSUTCDATETIME(),
                    UpdatedBy = @UpdatedBy
                WHERE Id = @Id AND IsDeleted = 0;";

            await using var command = new SqlCommand(sql, Connection, Transaction)
            {
                CommandTimeout = DefaultCommandTimeoutSeconds
            };
            command.Parameters.Add(new SqlParameter("@Id", SqlDbType.BigInt) { Value = id });
            command.Parameters.Add(new SqlParameter("@UpdatedBy", SqlDbType.NVarChar, 100) { Value = (object?)deletedBy ?? "system" });

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            return rowsAffected > 0;
        });
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            const string sql = @"SELECT COUNT(*) FROM dbo.Clients WHERE IsDeleted = 0;";
            await using var command = new SqlCommand(sql, Connection, Transaction)
            {
                CommandTimeout = DefaultCommandTimeoutSeconds
            };
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result);
        });
    }

    private static Client MapClient(SqlDataReader reader)
    {
        return new Client
        {
            Id = reader.GetInt64(reader.GetOrdinal("Id")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            FullName = reader.GetString(reader.GetOrdinal("FullName")),
            Phone = reader.GetString(reader.GetOrdinal("Phone")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            CreatedBy = reader.GetString(reader.GetOrdinal("CreatedBy")),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            UpdatedBy = reader.IsDBNull(reader.GetOrdinal("UpdatedBy")) ? null : reader.GetString(reader.GetOrdinal("UpdatedBy")),
            IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted"))
        };
    }
}
