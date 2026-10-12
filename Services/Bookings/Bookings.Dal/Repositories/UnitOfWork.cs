namespace Bookings.Dal.Repositories;

using System.Data;
using Microsoft.Data.SqlClient;
using Bookings.Dal.Interfaces;

/// <summary>
/// Реалізація патерну Unit of Work.
/// Забезпечує спільне SqlConnection та єдину SqlTransaction для всіх підлеглих репозиторіїв.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly SqlConnection _connection;
    private SqlTransaction? _transaction;
    private bool _disposed;

    private IClientRepository? _clients;
    private IAppointmentRepository? _appointments;
    private IPaymentRepository? _payments;
    private ICatalogServiceRepository? _catalogServices;

    public UnitOfWork(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Рядок підключення до БД не може бути порожнім.", nameof(connectionString));

        _connection = new SqlConnection(connectionString);
    }

    public UnitOfWork(SqlConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public IClientRepository Clients =>
        _clients ??= new ClientRepository(_connection, () => _transaction);

    public IAppointmentRepository Appointments =>
        _appointments ??= new AppointmentRepository(_connection, () => _transaction);

    public IPaymentRepository Payments =>
        _payments ??= new PaymentRepository(_connection, () => _transaction);

    public ICatalogServiceRepository CatalogServices =>
        _catalogServices ??= new CatalogServiceRepository(_connection, () => _transaction);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
            throw new InvalidOperationException("Транзакція вже активна в межах цього UnitOfWork.");

        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken);
        }

        _transaction = (SqlTransaction)await _connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("Немає активної транзакції для фіксації (Commit).");

        try
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            return;

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }

            await _connection.DisposeAsync();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
