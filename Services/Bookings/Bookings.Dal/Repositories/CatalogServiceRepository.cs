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
            }, Transaction, commandTimeout: DefaultCommandTimeoutSeconds, cancellationToken: cancellationToken);

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
            }, Transaction, commandTimeout: DefaultCommandTimeoutSeconds, cancellationToken: cancellationToken);

            var rows = await Connection.ExecuteAsync(command);
            return rows > 0;
        });
    }

    public async Task<IReadOnlyList<CatalogService>> GetByIdsAsync(IEnumerable<long> serviceIds, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<IReadOnlyList<CatalogService>>(async () =>
        {
            var idList = serviceIds?.Distinct().ToArray() ?? Array.Empty<long>();
            if (idList.Length == 0)
            {
                return Array.Empty<CatalogService>();
            }

            const string sql = @"
                SELECT Id, Name, Price, DurationMin, SyncedAt
                FROM dbo.Services
                WHERE Id IN @Ids;";

            var command = new CommandDefinition(
                sql,
                new { Ids = idList },
                Transaction,
                commandTimeout: DefaultCommandTimeoutSeconds,
                cancellationToken: cancellationToken);

            var results = await Connection.QueryAsync<CatalogService>(command);
            return results.AsList();
        });
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
            }, Transaction, commandTimeout: DefaultCommandTimeoutSeconds, cancellationToken: cancellationToken);

            await Connection.ExecuteAsync(command);
        });
    }

    public async Task<bool> UpdatePriceAsync(long serviceId, decimal newPrice, DateTime occurredAt, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            const string sql = @"
                UPDATE dbo.Services
                SET Price = @Price,
                    SyncedAt = @OccurredAt
                WHERE Id = @Id AND (SyncedAt IS NULL OR SyncedAt <= @OccurredAt);";

            var command = new CommandDefinition(sql, new
            {
                Id = serviceId,
                Price = newPrice,
                OccurredAt = occurredAt
            }, Transaction, commandTimeout: DefaultCommandTimeoutSeconds, cancellationToken: cancellationToken);

            var rows = await Connection.ExecuteAsync(command);
            return rows > 0;
        });
    }

    public async Task<bool> UpdateNameAsync(long serviceId, string newName, DateTime occurredAt, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            const string sql = @"
                UPDATE dbo.Services
                SET Name = @Name,
                    SyncedAt = @OccurredAt
                WHERE Id = @Id AND (SyncedAt IS NULL OR SyncedAt <= @OccurredAt);";

            var command = new CommandDefinition(sql, new
            {
                Id = serviceId,
                Name = newName,
                OccurredAt = occurredAt
            }, Transaction, commandTimeout: DefaultCommandTimeoutSeconds, cancellationToken: cancellationToken);

            var rows = await Connection.ExecuteAsync(command);
            return rows > 0;
        });
    }

    public async Task<bool> UpsertReplicaAsync(long serviceId, string name, decimal price, int durationMin, DateTime occurredAt, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            const string sql = @"
                MERGE INTO dbo.Services AS target
                USING (SELECT @Id AS Id, @Name AS Name, @Price AS Price, @DurationMin AS DurationMin, @OccurredAt AS OccurredAt) AS source
                ON (target.Id = source.Id)
                WHEN MATCHED AND (target.SyncedAt IS NULL OR target.SyncedAt <= source.OccurredAt) THEN
                    UPDATE SET target.Name = source.Name,
                               target.Price = CASE WHEN source.Price > 0 THEN source.Price ELSE target.Price END,
                               target.DurationMin = CASE WHEN source.DurationMin > 0 THEN source.DurationMin ELSE target.DurationMin END,
                               target.SyncedAt = source.OccurredAt
                WHEN NOT MATCHED THEN
                    INSERT (Id, Name, Price, DurationMin, SyncedAt)
                    VALUES (source.Id, source.Name, source.Price, source.DurationMin, source.OccurredAt);";

            var command = new CommandDefinition(sql, new
            {
                Id = serviceId,
                Name = name,
                Price = price,
                DurationMin = durationMin,
                OccurredAt = occurredAt
            }, Transaction, commandTimeout: DefaultCommandTimeoutSeconds, cancellationToken: cancellationToken);

            var rows = await Connection.ExecuteAsync(command);
            return rows > 0;
        });
    }
}
