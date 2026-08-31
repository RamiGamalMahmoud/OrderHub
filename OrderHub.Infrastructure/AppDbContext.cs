using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Models;
using OrderHub.Domain.Models.CommercialDocuments;
using OrderHub.Infrastructure.Configurations;
using OrderHub.Infrastructure.Models;

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
    public DbSet<OrderEntitySequence> OrderEntitySequences { get; set; }
    public DbSet<OrderDeliveryStep> OrderDeliverySteps { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    public DbSet<OutboxMessageAttachment> MessageAttachments { get; set; }
    public DbSet<ClientRecipient> ClientRecipients { get; set; }
    public DbSet<SupplierRecipient> SupplierRecipients { get; set; }
    public DbSet<DeliverymanRecipient> DeliverymanRecipients { get; set; }
    public DbSet<ShippingCarrierRecipient> ShippingCarrierRecipients { get; set; }
    public DbSet<WhatsappGroup> WhatsappGroups { get; set; }
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IModelsConfigurationMarker).Assembly);
    }
}
