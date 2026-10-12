namespace Bookings.Dal.Common;

using System.Data;
using Microsoft.Data.SqlClient;
using Bookings.Domain.Exceptions;

/// <summary>
/// Базовий клас для всіх репозиторіїв (чистий ADO.NET та Dapper).
/// Забезпечує доступ до активного з'єднання, поточної транзакції та трансляцію помилок БД у доменні винятки.
/// </summary>
public abstract class RepositoryBase
{
    protected readonly SqlConnection Connection;
    private readonly Func<SqlTransaction?> _transactionProvider;

    protected SqlTransaction? Transaction => _transactionProvider();

    protected RepositoryBase(SqlConnection connection, Func<SqlTransaction?> transactionProvider)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _transactionProvider = transactionProvider ?? throw new ArgumentNullException(nameof(transactionProvider));
    }

    /// <summary>
    /// Переконується, що з'єднання відкрите перед виконанням команди.
    /// </summary>
    protected async Task EnsureConnectionOpenAsync(CancellationToken cancellationToken = default)
    {
        if (Connection.State != ConnectionState.Open)
        {
            await Connection.OpenAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Виконання операції з автоматичним перехопленням та трансляцією SQL-помилок.
    /// </summary>
    protected async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action)
    {
        try
        {
            await EnsureConnectionOpenAsync();
            return await action();
        }
        catch (SqlException ex)
        {
            throw TranslateSqlException(ex);
        }
    }

    /// <summary>
    /// Виконання void-операції з автоматичним перехопленням та трансляцією SQL-помилок.
    /// </summary>
    protected async Task ExecuteAsync(Func<Task> action)
    {
        try
        {
            await EnsureConnectionOpenAsync();
            await action();
        }
        catch (SqlException ex)
        {
            throw TranslateSqlException(ex);
        }
    }

    /// <summary>
    /// Трансляція кодів помилок SQL Server та користувацьких THROW у строго типізовані доменні винятки.
    /// </summary>
    protected Exception TranslateSqlException(SqlException ex)
    {
        return ex.Number switch
        {
            // Користувацькі коди збережених процедур
            50001 => new NotFoundException(ex.Message, ex),
            50002 => new BusinessConflictException(ex.Message, ex),
            50003 => new BusinessConflictException(ex.Message, ex),
            50004 => new BusinessConflictException(ex.Message, ex),
            50005 => new ValidationException(ex.Message),

            // Стандартні коди обмежень SQL Server
            2601 or 2627 => new BusinessConflictException("Запис з таким унікальним значенням вже існує в системі.", ex),
            547 => new BusinessConflictException("Порушення цілісності даних (зовнішній ключ або обмеження перевірки CHECK).", ex),

            _ => ex
        };
    }
}
