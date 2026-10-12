namespace Bookings.Dal.Repositories;

using Dapper;
using Microsoft.Data.SqlClient;
using Bookings.Dal.Interfaces;
using Bookings.Domain.Models;

/// <summary>
/// Репозиторій для роботи з локальною реплікою довідника послуг (таблиця dbo.Services).
/// Працює за схемою Read-Model (зберігає копію послуг з контексту Catalog).
/// </summary>
public class CatalogServiceRepository : BaseDapperRepository<CatalogService>, ICatalogServiceRepository
{
    protected override string TableName => "dbo.Services";
    protected override bool HasSoftDelete => false;

    public CatalogServiceRepository(SqlConnection connection, Func<SqlTransaction?> transactionProvider)
        : base(connection, transactionProvider)
    {
    }

    public override async Task<long> AddAsync(CatalogService entity, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            const string sql = @"
                INSERT INTO dbo.Services (Id, Name, Price, DurationMin, SyncedAt)
                VALUES (@Id, @Name, @Price, @DurationMin, SYSUTCDATETIME());";

            var command = new CommandDefinition(sql, new
            {
                entity.Id,
                entity.Name,
                entity.Price,
                entity.DurationMin
            }, Transaction, cancellationToken: cancellationToken);

            await Connection.ExecuteAsync(command);
            return entity.Id;
        });
    }

    public override async Task<bool> UpdateAsync(CatalogService entity, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            const string sql = @"
                UPDATE dbo.Services
                SET Name = @Name,
                    Price = @Price,
                    DurationMin = @DurationMin,
                    SyncedAt = SYSUTCDATETIME()
                WHERE Id = @Id;";

            var command = new CommandDefinition(sql, new
            {
                entity.Id,
                entity.Name,
                entity.Price,
                entity.DurationMin
            }, Transaction, cancellationToken: cancellationToken);

            var rows = await Connection.ExecuteAsync(command);
            return rows > 0;
        });
    }

    public async Task<CatalogService?> GetByServiceIdAsync(long serviceId, CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync(serviceId, cancellationToken);
    }

    public async Task UpsertAsync(CatalogService service, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            const string sql = @"
                MERGE INTO dbo.Services AS target
                USING (SELECT @Id AS Id, @Name AS Name, @Price AS Price, @DurationMin AS DurationMin) AS source
                ON (target.Id = source.Id)
                WHEN MATCHED THEN
                    UPDATE SET target.Name = source.Name,
                               target.Price = source.Price,
                               target.DurationMin = source.DurationMin,
                               target.SyncedAt = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT (Id, Name, Price, DurationMin, SyncedAt)
                    VALUES (source.Id, source.Name, source.Price, source.DurationMin, SYSUTCDATETIME());";

            var command = new CommandDefinition(sql, new
            {
                service.Id,
                service.Name,
                service.Price,
                service.DurationMin
            }, Transaction, cancellationToken: cancellationToken);

            await Connection.ExecuteAsync(command);
        });
    }
}
