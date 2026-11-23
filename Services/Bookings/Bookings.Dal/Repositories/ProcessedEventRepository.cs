namespace Bookings.Dal.Repositories;

using Dapper;
using Microsoft.Data.SqlClient;
using Bookings.Dal.Interfaces;

/// <summary>
/// Репозиторій для роботи з таблицею dbo.ProcessedEvents (Критерій 9).
/// </summary>
public class ProcessedEventRepository : IProcessedEventRepository
{
    private readonly SqlConnection _connection;
    private readonly Func<SqlTransaction?> _transactionProvider;

    public ProcessedEventRepository(SqlConnection connection, Func<SqlTransaction?> transactionProvider)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _transactionProvider = transactionProvider ?? throw new ArgumentNullException(nameof(transactionProvider));
    }

    public async Task<bool> TryAddAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO dbo.ProcessedEvents (EventId, ProcessedAt)
            VALUES (@EventId, SYSUTCDATETIME());";

        var command = new CommandDefinition(
            sql,
            new { EventId = eventId },
            _transactionProvider(),
            commandTimeout: 30,
            cancellationToken: cancellationToken);

        try
        {
            await _connection.ExecuteAsync(command);
            return true;
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            // Унікальне обмеження первинного ключа порушено -> повторна доставка дубліката
            return false;
        }
    }

    public async Task<bool> HasBeenProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 1 FROM dbo.ProcessedEvents WHERE EventId = @EventId;";

        var command = new CommandDefinition(
            sql,
            new { EventId = eventId },
            _transactionProvider(),
            commandTimeout: 30,
            cancellationToken: cancellationToken);

        var result = await _connection.ExecuteScalarAsync<int?>(command);
        return result.HasValue;
    }
}
