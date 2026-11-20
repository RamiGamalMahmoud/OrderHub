using System.Data.Common;
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
        PostgresTestDatabase.Reset();
        var options = PostgresTestDatabase.Options;
        using var context = new AppDbContext(options);
        var (client, supplier) = SeedParents(context);

        var supply = NewNumberedSupply(client, supplier, "ORD-PERSIST");
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
        PostgresTestDatabase.Reset();
        var interceptor = new FailHistoryInsertInterceptor();
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(PostgresTestDatabase.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(interceptor);
        var options = optionsBuilder.Options;
        Client client;
        Supplier supplier;
        using (var seedContext = new AppDbContext(options))
        {
            (client, supplier) = SeedParents(seedContext);
        }

        using (var context = new AppDbContext(options))
        {
            var supply = NewNumberedSupply(client, supplier, "ORD-ROLLBACK");
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

    private static (Client Client, Supplier Supplier) SeedParents(AppDbContext context)
    {
        var city = new City("الرياض");
        var client = new Client("عميل", "شارع الملك", city, "0501111111", "+966");
        var supplier = new Supplier("مورد");
        context.Cities.Add(city);
        context.Clients.Add(client);
        context.Suppliers.Add(supplier);
        context.SaveChanges();
        return (client, supplier);
    }

    private static Supply NewNumberedSupply(Client client, Supplier supplier, string orderNumber)
    {
        var supply = new Supply(new Order(client.Id, orderNumber), supplier.Id);
        ((IWorkRecord)supply).AssignWorkRecordNumber(supplier.Id, 1, 2026, 10);
        return supply;
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
            if (Enabled && command.CommandText.Contains("work_record_status_histories", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Injected status history database failure.");
        }
    }
}
