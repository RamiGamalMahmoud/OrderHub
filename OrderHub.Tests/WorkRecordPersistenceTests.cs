using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure;

namespace OrderHub.Tests;

public sealed class WorkRecordPersistenceTests
{
    [Fact]
    public void Saving_a_new_work_record_persists_its_pending_history_atomically()
    {
        using var connection = OpenConnection();
        var options = Options(connection);
        using var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        var supply = NewNumberedSupply("ORD-PERSIST");
        context.Supplies.Add(supply);
        context.SaveChanges();

        var initial = Assert.Single(context.WorkRecordStatusHistories);
        Assert.True(supply.Id > 0);
        Assert.Equal(WorkRecordType.SupplierSupply, initial.WorkRecordType);
        Assert.Equal(supply.Id, initial.WorkRecordId);
        Assert.Equal(WorkRecordStatus.Pending, initial.Status);
        Assert.Equal(WorkRecordStatus.Pending, context.Supplies.Single().Status);
    }

    [Fact]
    public void Failed_history_insert_rolls_back_the_current_status_update()
    {
        using var connection = OpenConnection();
        var interceptor = new FailHistoryInsertInterceptor();
        var options = Options(connection, interceptor);
        using (var context = new AppDbContext(options))
        {
            context.Database.EnsureCreated();
            var supply = NewNumberedSupply("ORD-ROLLBACK");
            context.Supplies.Add(supply);
            context.SaveChanges();

            supply.TransitionTo(WorkRecordStatus.Failed, "Supplier unavailable", DateTime.UtcNow);
            interceptor.Enabled = true;
            Assert.Throws<DbUpdateException>(() => context.SaveChanges());
        }

        interceptor.Enabled = false;
        using var verification = new AppDbContext(options);
        Assert.Equal(WorkRecordStatus.Pending, verification.Supplies.Single().Status);
        var history = Assert.Single(verification.WorkRecordStatusHistories);
        Assert.Equal(WorkRecordStatus.Pending, history.Status);
    }

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = OFF";
        command.ExecuteNonQuery();
        return connection;
    }

    private static Supply NewNumberedSupply(string orderNumber)
    {
        var supply = new Supply(new Order(10, orderNumber), 20);
        ((IWorkRecord)supply).AssignWorkRecordNumber(20, 1, 2026, 10);
        return supply;
    }

    private static DbContextOptions<AppDbContext> Options(SqliteConnection connection, DbCommandInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .UseSnakeCaseNamingConvention();
        if (interceptor is not null) builder.AddInterceptors(interceptor);
        return builder.Options;
    }

    private sealed class FailHistoryInsertInterceptor : DbCommandInterceptor
    {
        public bool Enabled { get; set; }

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            ThrowIfHistoryInsert(command);
            return result;
        }

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result)
        {
            ThrowIfHistoryInsert(command);
            return result;
        }

        private void ThrowIfHistoryInsert(DbCommand command)
        {
            if (Enabled && command.CommandText.Contains("INSERT INTO \"work_record_status_histories\"", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Injected status history database failure.");
        }
    }
}
