using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Models;
using OrderHub.Domain.Models.CommercialDocuments;
using OrderHub.Infrastructure.Configurations;
using OrderHub.Infrastructure.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure;

internal class AppDbContext(DbContextOptions dbContextOptions) : DbContext(dbContextOptions)
{
    public DbSet<City> Cities { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<Phone> Phones { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Deliveryman> Deliverymen {  get; set; }
    public DbSet<ShippingCarrier> ShippingCarriers { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDeliveryStep> OrderDeliverySteps { get; set; }
    public DbSet<Supply> Supplies { get; set; }
    public DbSet<Shipment> Shipments { get; set; }
    public DbSet<DeliveryTrip> DeliveryTrips { get; set; }
    public DbSet<WorkRecordStatusHistory> WorkRecordStatusHistories { get; set; }
    public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
    public DbSet<DeliveryStepStatusHistory> DeliveryStepStatusHistories { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }
    public DbSet<Property> Properties { get; set; }
    public DbSet<PropertyOption> PropertyOptions { get; set; }
    public DbSet<ProductProperty> ProductProperties { get; set; }
    public DbSet<OrderItemProperty> OrderItemProperties { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceItem> InvoicesItem { get; set; }
    public DbSet<DocumentSequence>  DocumentSequences { get; set; }
    public DbSet<Quotation> Quotations { get; set; }
    public DbSet<QuotationItem> QuotationItems { get; set; }
    public DbSet<ProformaInvoice> ProformaInvoices { get; set; }
    public DbSet<ProformaInvoiceItem> ProformaInvoiceItems { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<DraftRecord> DraftRecords { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<NumberSequence> NumberSequences { get; set; }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ProtectHistory();
        return SaveWorkRecordsAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ProtectHistory();
        return SaveWorkRecords(acceptAllChangesOnSuccess);
    }

    private void ProtectHistory()
    {
        if (ChangeTracker.Entries().Any(entry => entry.Entity is OrderStatusHistory or DeliveryStepStatusHistory or WorkRecordStatusHistory &&
            entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("سجل التشغيل محفوظ ولا يمكن تعديله أو حذفه.");
    }

    private async Task<int> SaveWorkRecordsAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken)
    {
        var added = ChangeTracker.Entries<IWorkRecord>().Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity).ToList();
        var tracked = ChangeTracker.Entries<IWorkRecord>().Select(entry => entry.Entity).Distinct().ToList();
        var hasWorkRecordChanges = added.Count > 0 || tracked.Any(record => record.UnpersistedStatusHistory.Any());
        var ownTransaction = hasWorkRecordChanges && Database.CurrentTransaction is null;
        await using var transaction = ownTransaction ? await Database.BeginTransactionAsync(cancellationToken) : null;
        try
        {
            var affected = await base.SaveChangesAsync(added.Count > 0 || acceptAllChangesOnSuccess, cancellationToken);
            foreach (var record in added)
            {
                record.AppendInitialStatusHistory();
                foreach (var history in record.UnpersistedStatusHistory)
                    if (history.WorkRecordId == 0) history.SetWorkRecordId(((ModelBase)record).Id);
            }
            AddPendingWorkHistory(tracked);
            if (ChangeTracker.Entries<WorkRecordStatusHistory>().Any(entry => entry.State == EntityState.Added))
                affected += await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            if (ownTransaction) await transaction!.CommitAsync(cancellationToken);
            foreach (var record in tracked) record.MarkStatusHistoryPersisted();
            return affected;
        }
        catch
        {
            if (ownTransaction) await transaction!.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private int SaveWorkRecords(bool acceptAllChangesOnSuccess)
    {
        var added = ChangeTracker.Entries<IWorkRecord>().Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity).ToList();
        var tracked = ChangeTracker.Entries<IWorkRecord>().Select(entry => entry.Entity).Distinct().ToList();
        var hasWorkRecordChanges = added.Count > 0 || tracked.Any(record => record.UnpersistedStatusHistory.Any());
        using var transaction = hasWorkRecordChanges && Database.CurrentTransaction is null ? Database.BeginTransaction() : null;
        try
        {
            var affected = base.SaveChanges(added.Count > 0 || acceptAllChangesOnSuccess);
            foreach (var record in added)
            {
                record.AppendInitialStatusHistory();
                foreach (var history in record.UnpersistedStatusHistory)
                    if (history.WorkRecordId == 0) history.SetWorkRecordId(((ModelBase)record).Id);
            }
            AddPendingWorkHistory(tracked);
            if (ChangeTracker.Entries<WorkRecordStatusHistory>().Any(entry => entry.State == EntityState.Added))
                affected += base.SaveChanges(acceptAllChangesOnSuccess);
            transaction?.Commit();
            foreach (var record in tracked) record.MarkStatusHistoryPersisted();
            return affected;
        }
        catch
        {
            transaction?.Rollback();
            throw;
        }
    }

    private void AddPendingWorkHistory(IEnumerable<IWorkRecord> records)
    {
        foreach (var history in records.SelectMany(record => record.UnpersistedStatusHistory))
            if (!ChangeTracker.Entries<WorkRecordStatusHistory>().Any(entry => ReferenceEquals(entry.Entity, history)))
                WorkRecordStatusHistories.Add(history);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IModelsConfigurationMarker).Assembly);
    }
}
