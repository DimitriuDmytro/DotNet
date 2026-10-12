namespace Bookings.Dal.Repositories;

using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Bookings.Dal.Interfaces;
using Bookings.Domain.Models;

/// <summary>
/// Репозиторій платежів із використанням Dapper та збереженої процедури usp_RegisterPayment.
/// </summary>
public class PaymentRepository : BaseDapperRepository<Payment, long>, IPaymentRepository
{
    protected override string TableName => "dbo.Payments";
    protected override bool HasSoftDelete => false;

    public PaymentRepository(SqlConnection connection, Func<SqlTransaction?> transactionProvider)
        : base(connection, transactionProvider)
    {
    }

    public override async Task<long> AddAsync(Payment entity, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            const string sql = @"
                INSERT INTO dbo.Payments (AppointmentId, Amount, Method, PaidAt)
                VALUES (@AppointmentId, @Amount, @Method, SYSUTCDATETIME());
                SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

            var command = new CommandDefinition(sql, new
            {
                entity.AppointmentId,
                entity.Amount,
                entity.Method
            }, Transaction, cancellationToken: cancellationToken);

            return await Connection.ExecuteScalarAsync<long>(command);
        });
    }

    public override async Task<bool> UpdateAsync(Payment entity, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            const string sql = @"
                UPDATE dbo.Payments
                SET Amount = @Amount,
                    Method = @Method
                WHERE Id = @Id;";

            var command = new CommandDefinition(sql, new
            {
                entity.Id,
                entity.Amount,
                entity.Method
            }, Transaction, cancellationToken: cancellationToken);

            var rows = await Connection.ExecuteAsync(command);
            return rows > 0;
        });
    }

    public async Task<IReadOnlyList<Payment>> GetByAppointmentIdAsync(long appointmentId, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            const string sql = @"
                SELECT Id, AppointmentId, Amount, Method, PaidAt
                FROM dbo.Payments
                WHERE AppointmentId = @AppointmentId
                ORDER BY PaidAt ASC, Id ASC;";

            var command = new CommandDefinition(sql, new { AppointmentId = appointmentId }, Transaction, cancellationToken: cancellationToken);
            var results = await Connection.QueryAsync<Payment>(command);
            return results.AsList();
        });
    }

    public async Task RegisterPaymentViaProcedureAsync(long appointmentId, decimal amount, string method, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            var parameters = new DynamicParameters();
            parameters.Add("@AppointmentId", appointmentId, DbType.Int64);
            parameters.Add("@Amount", amount, DbType.Decimal, precision: 18, scale: 2);
            parameters.Add("@Method", method, DbType.String, size: 30);

            var command = new CommandDefinition(
                "dbo.usp_RegisterPayment",
                parameters,
                Transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            await Connection.ExecuteAsync(command);
        });
    }

    public async Task<decimal> GetTotalPaidForAppointmentAsync(long appointmentId, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            const string sql = @"
                SELECT ISNULL(SUM(Amount), 0.00)
                FROM dbo.Payments
                WHERE AppointmentId = @AppointmentId;";

            var command = new CommandDefinition(sql, new { AppointmentId = appointmentId }, Transaction, cancellationToken: cancellationToken);
            return await Connection.ExecuteScalarAsync<decimal>(command);
        });
    }
}
