using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Enums;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace OrderHub.Infrastructure.Services;

internal sealed class NumberSequenceService : INumberSequenceService
{
    private const string INCREMENT_SQL = """
        INSERT INTO number_sequences
            (kind, party_id, year, month, last_number)
        VALUES
            (@kind, @party_id, @year, @month, 1)
        ON CONFLICT (kind, party_id, year, month)
        DO UPDATE SET
            last_number = number_sequences.last_number + 1
        RETURNING last_number;
        """;

    private static readonly TimeZoneInfo _riyadhTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Riyadh");

    private readonly AppDbContext _dbContext;
    private readonly AppDbContextFactory _dbContextFactory;

    public NumberSequenceService(
        AppDbContext dbContext,
        AppDbContextFactory dbContextFactory)
    {
        _dbContext = dbContext;
        _dbContextFactory = dbContextFactory;
    }


    public async Task<long> NextGlobalAsync(
        NumberSequenceKind kind,
        CancellationToken cancellationToken = default)
    {
        if (kind != NumberSequenceKind.Order)
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Global numbering is supported only for orders.");
        }

        using var transactionScope = new TransactionScope(
            TransactionScopeOption.Suppress,
            TransactionScopeAsyncFlowOption.Enabled);

        await using var dbContext = _dbContextFactory.CreateDbContext();

        var number = await IncrementAsync(
            dbContext,
            kind,
            0,
            0,
            0,
            cancellationToken);

        transactionScope.Complete();

        return number;
    }


    public async Task<MonthlyNumberAllocation> NextMonthlyAsync(
        NumberSequenceKind kind,
        int partyId,
        DateTimeOffset atUtc,
        CancellationToken cancellationToken = default)
    {
        if (kind is not (
            NumberSequenceKind.Supplier
            or NumberSequenceKind.Deliveryman
            or NumberSequenceKind.ShippingCarrier))
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Monthly numbering is supported only for parties.");
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(partyId);

        var riyadhTime = TimeZoneInfo.ConvertTime(atUtc, _riyadhTimeZone);

        var number = await IncrementAsync(
            _dbContext,
            kind,
            partyId,
            riyadhTime.Year,
            riyadhTime.Month,
            cancellationToken);

        return new MonthlyNumberAllocation(number, riyadhTime.Year, riyadhTime.Month);
    }

    private static async Task<long> IncrementAsync(
        AppDbContext dbContext,
        NumberSequenceKind kind,
        int partyId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();

            command.CommandText = INCREMENT_SQL;

            var currentTransaction = dbContext.Database.CurrentTransaction;

            if (currentTransaction is not null)
            {
                command.Transaction = currentTransaction.GetDbTransaction();
            }

            AddParameter(command, "kind", kind.ToString());
            AddParameter(command, "party_id", partyId);
            AddParameter(command, "year", year);
            AddParameter(command, "month", month);

            var result = await command.ExecuteScalarAsync(cancellationToken);

            return Convert.ToInt64(result);
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static void AddParameter(
        System.Data.Common.DbCommand command,
        string name,
        object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
