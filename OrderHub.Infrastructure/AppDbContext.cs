using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Models;
using OrderHub.Domain.Models.CommercialDocuments;
using OrderHub.Infrastructure.Configurations;
using OrderHub.Infrastructure.Models;
using System;
using System.Linq;
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
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ProtectHistory();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    private void ProtectHistory()
    {
        if (ChangeTracker.Entries().Any(entry => entry.Entity is OrderStatusHistory or DeliveryStepStatusHistory &&
            entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("سجل التشغيل محفوظ ولا يمكن تعديله أو حذفه.");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IModelsConfigurationMarker).Assembly);
    }
}
