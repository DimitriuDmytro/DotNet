namespace Bookings.Dal.Repositories;

using Dapper;
using Microsoft.Data.SqlClient;
using Bookings.Dal.Common;
using Bookings.Dal.Interfaces;

/// <summary>
/// Базова реалізація IGenericRepository<T> з ключем типу long за допомогою Dapper.
/// </summary>
public abstract class BaseDapperRepository<T> : BaseDapperRepository<T, long>, IGenericRepository<T> where T : class
{
    protected BaseDapperRepository(SqlConnection connection, Func<SqlTransaction?> transactionProvider)
        : base(connection, transactionProvider)
    {
    }
}

/// <summary>
/// Базова реалізація IGenericRepository<T, TKey> за допомогою Dapper.
/// Забезпечує стандартні CRUD-операції з автоматичною фільтрацією за IsDeleted.
/// </summary>
public abstract class BaseDapperRepository<T, TKey> : RepositoryBase, IGenericRepository<T, TKey> where T : class
{
    protected abstract string TableName { get; }
    protected virtual string KeyColumnName => "Id";
    protected virtual bool HasSoftDelete => true;

    protected BaseDapperRepository(SqlConnection connection, Func<SqlTransaction?> transactionProvider)
        : base(connection, transactionProvider)
    {
    }

    public virtual async Task<T?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            var filter = HasSoftDelete ? "AND IsDeleted = 0" : string.Empty;
            var sql = $"SELECT * FROM {TableName} WHERE {KeyColumnName} = @Id {filter};";

            var command = new CommandDefinition(
                sql,
                new { Id = id },
                Transaction,
                commandTimeout: DefaultCommandTimeoutSeconds,
                cancellationToken: cancellationToken);
            return await Connection.QuerySingleOrDefaultAsync<T>(command);
        });
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            var filter = HasSoftDelete ? "WHERE IsDeleted = 0" : string.Empty;
            var sql = $"SELECT * FROM {TableName} {filter};";

            var command = new CommandDefinition(
                sql,
                transaction: Transaction,
                commandTimeout: DefaultCommandTimeoutSeconds,
                cancellationToken: cancellationToken);
            var results = await Connection.QueryAsync<T>(command);
            return results.AsList();
        });
    }

    public abstract Task<TKey> AddAsync(T entity, CancellationToken cancellationToken = default);

    public abstract Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default);

    public virtual async Task<bool> DeleteAsync(TKey id, string? deletedBy = null, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            if (HasSoftDelete)
            {
                var sql = $@"
                    UPDATE {TableName}
                    SET IsDeleted = 1,
                        UpdatedAt = SYSUTCDATETIME(),
                        UpdatedBy = @UpdatedBy
                    WHERE {KeyColumnName} = @Id AND IsDeleted = 0;";

                var command = new CommandDefinition(
                    sql,
                    new { Id = id, UpdatedBy = deletedBy ?? "system" },
                    Transaction,
                    commandTimeout: DefaultCommandTimeoutSeconds,
                    cancellationToken: cancellationToken);
                var rows = await Connection.ExecuteAsync(command);
                return rows > 0;
            }
            else
            {
                var sql = $"DELETE FROM {TableName} WHERE {KeyColumnName} = @Id;";
                var command = new CommandDefinition(
                    sql,
                    new { Id = id },
                    Transaction,
                    commandTimeout: DefaultCommandTimeoutSeconds,
                    cancellationToken: cancellationToken);
                var rows = await Connection.ExecuteAsync(command);
                return rows > 0;
            }
        });
    }
}
