namespace Bookings.Dal.Repositories;

using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Bookings.Dal.Interfaces;
using Bookings.Domain.Models;

/// <summary>
/// Репозиторій записів на обслуговування з використанням Dapper, Multi-mapping,
/// Table-Valued Parameters (TVP) та викликів збережених процедур.
/// </summary>
public class AppointmentRepository : BaseDapperRepository<Appointment>, IAppointmentRepository
{
    protected override string TableName => "dbo.Appointments";

    public AppointmentRepository(SqlConnection connection, Func<SqlTransaction?> transactionProvider)
        : base(connection, transactionProvider)
    {
    }

    public override async Task<long> AddAsync(Appointment entity, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            const string sql = @"
                INSERT INTO dbo.Appointments (ClientId, Status, ScheduledAt, TotalAmount, CreatedAt, CreatedBy, IsDeleted)
                VALUES (@ClientId, @Status, @ScheduledAt, @TotalAmount, SYSUTCDATETIME(), @CreatedBy, 0);
                SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

            var command = new CommandDefinition(sql, new
            {
                entity.ClientId,
                entity.Status,
                entity.ScheduledAt,
                entity.TotalAmount,
                CreatedBy = entity.CreatedBy ?? "system"
            }, Transaction, commandTimeout: DefaultCommandTimeoutSeconds, cancellationToken: cancellationToken);

            return await Connection.ExecuteScalarAsync<long>(command);
        });
    }

    public override async Task<bool> UpdateAsync(Appointment entity, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            const string sql = @"
                UPDATE dbo.Appointments
                SET ScheduledAt = @ScheduledAt,
                    Status = @Status,
                    TotalAmount = @TotalAmount,
                    UpdatedAt = SYSUTCDATETIME(),
                    UpdatedBy = @UpdatedBy
                WHERE Id = @Id AND IsDeleted = 0;";

            var command = new CommandDefinition(sql, new
            {
                entity.Id,
                entity.ScheduledAt,
                entity.Status,
                entity.TotalAmount,
                entity.UpdatedBy
            }, Transaction, commandTimeout: DefaultCommandTimeoutSeconds, cancellationToken: cancellationToken);

            var rows = await Connection.ExecuteAsync(command);
            return rows > 0;
        });
    }

    public async Task<Appointment?> GetDetailedByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            const string sqlAppointment = @"
                SELECT a.Id, a.ClientId, a.Status, a.ScheduledAt, a.TotalAmount, 
                       a.CreatedAt, a.CreatedBy, a.UpdatedAt, a.UpdatedBy, a.IsDeleted, a.RowVersion,
                       d.AppointmentId, d.CarBrand, d.CarModel, d.PlateNumber, d.Comment
                FROM dbo.Appointments a
                LEFT JOIN dbo.AppointmentDetails d ON a.Id = d.AppointmentId
                WHERE a.Id = @Id AND a.IsDeleted = 0;";

            var command = new CommandDefinition(
                sqlAppointment,
                new { Id = id },
                Transaction,
                commandTimeout: DefaultCommandTimeoutSeconds,
                cancellationToken: cancellationToken);

            var appointments = (await Connection.QueryAsync<Appointment, AppointmentDetails, Appointment>(
                command,
                (app, det) =>
                {
                    app.Details = det;
                    return app;
                },
                splitOn: "AppointmentId"
            )).AsList();

            var appointment = appointments.FirstOrDefault();
            if (appointment != null)
            {
                const string sqlItems = @"
                    SELECT AppointmentId, ServiceId, ServiceName, UnitPrice, Discount, Quantity
                    FROM dbo.AppointmentItems
                    WHERE AppointmentId = @Id;";

                var itemsCommand = new CommandDefinition(sqlItems, new { Id = id }, Transaction, cancellationToken: cancellationToken);
                var items = (await Connection.QueryAsync<AppointmentItem>(itemsCommand)).AsList();
                appointment.Items = items;
            }

            return appointment;
        });
    }

    public async Task<IReadOnlyList<Appointment>> GetByClientIdAsync(
        long clientId,
        string? status = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            var parameters = new DynamicParameters();
            parameters.Add("@ClientId", clientId, DbType.Int64);
            parameters.Add("@Status", status, DbType.String, size: 30);
            parameters.Add("@PageNumber", pageNumber, DbType.Int32);
            parameters.Add("@PageSize", pageSize, DbType.Int32);

            var command = new CommandDefinition(
                "dbo.usp_GetClientAppointments",
                parameters,
                Transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            var results = await Connection.QueryAsync<Appointment>(command);
            return results.AsList();
        });
    }

    public async Task<(long AppointmentId, decimal TotalAmount, string Status)> CreateViaProcedureAsync(
        long clientId,
        DateTime scheduledAt,
        string carBrand,
        string carModel,
        string plateNumber,
        string? comment,
        string createdBy,
        IEnumerable<(long ServiceId, int Quantity, decimal Discount)> items,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            var tvpTable = new DataTable();
            tvpTable.Columns.Add("ServiceId", typeof(long));
            tvpTable.Columns.Add("Quantity", typeof(int));
            tvpTable.Columns.Add("Discount", typeof(decimal));

            foreach (var item in items)
            {
                tvpTable.Rows.Add(item.ServiceId, item.Quantity, item.Discount);
            }

            var parameters = new DynamicParameters();
            parameters.Add("@ClientId", clientId, DbType.Int64);
            parameters.Add("@ScheduledAt", scheduledAt, DbType.DateTime2);
            parameters.Add("@CarBrand", carBrand, DbType.String, size: 100);
            parameters.Add("@CarModel", carModel, DbType.String, size: 100);
            parameters.Add("@PlateNumber", plateNumber, DbType.String, size: 20);
            parameters.Add("@Comment", comment, DbType.String, size: 1000);
            parameters.Add("@CreatedBy", createdBy ?? "system", DbType.String, size: 100);
            parameters.Add("@Items", tvpTable.AsTableValuedParameter("dbo.AppointmentItemTableType"));
            parameters.Add("@NewAppointmentId", dbType: DbType.Int64, direction: ParameterDirection.Output);

            var command = new CommandDefinition(
                "dbo.usp_CreateAppointment",
                parameters,
                Transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            var row = await Connection.QueryFirstOrDefaultAsync<dynamic>(command);
            long id = row != null ? (long)row.AppointmentId : parameters.Get<long>("@NewAppointmentId");
            decimal total = row != null ? (decimal)row.TotalAmount : 0m;
            string stat = row != null ? (string)row.Status : "Booked";

            return (id, total, stat);
        });
    }

    public async Task ChangeStatusViaProcedureAsync(
        long appointmentId,
        string newStatus,
        string changedBy,
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            var parameters = new DynamicParameters();
            parameters.Add("@AppointmentId", appointmentId, DbType.Int64);
            parameters.Add("@NewStatus", newStatus, DbType.String, size: 30);
            parameters.Add("@ChangedBy", changedBy ?? "system", DbType.String, size: 100);

            var command = new CommandDefinition(
                "dbo.usp_ChangeStatus",
                parameters,
                Transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            await Connection.ExecuteAsync(command);
        });
    }

    public async Task DeliverCarViaProcedureAsync(
        long appointmentId,
        string changedBy,
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            var parameters = new DynamicParameters();
            parameters.Add("@AppointmentId", appointmentId, DbType.Int64);
            parameters.Add("@ChangedBy", changedBy ?? "system", DbType.String, size: 100);

            var command = new CommandDefinition(
                "dbo.usp_DeliverCar",
                parameters,
                Transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            await Connection.ExecuteAsync(command);
        });
    }

    public async Task CancelViaProcedureAsync(
        long appointmentId,
        string changedBy,
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            var parameters = new DynamicParameters();
            parameters.Add("@AppointmentId", appointmentId, DbType.Int64);
            parameters.Add("@ChangedBy", changedBy ?? "system", DbType.String, size: 100);

            var command = new CommandDefinition(
                "dbo.usp_CancelAppointment",
                parameters,
                Transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            await Connection.ExecuteAsync(command);
        });
    }

    public async Task<IReadOnlyList<StatusHistoryEntry>> GetStatusHistoryAsync(
        long appointmentId,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            const string sql = @"
                SELECT Id, AppointmentId, OldStatus, NewStatus, ChangedAt, ChangedBy
                FROM dbo.AppointmentStatusHistory
                WHERE AppointmentId = @AppointmentId
                ORDER BY ChangedAt ASC, Id ASC;";

            var command = new CommandDefinition(sql, new { AppointmentId = appointmentId }, Transaction, cancellationToken: cancellationToken);
            var results = await Connection.QueryAsync<StatusHistoryEntry>(command);
            return results.AsList();
        });
    }
}
