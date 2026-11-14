using OrderHub.Application.Features.Orders.GetForEdit;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Enums;

namespace OrderHub.Infrastructure.Features.Orders;

internal sealed partial class OrderReader
{
    public async Task<OrderEditorData> GetForEditAsync(int orderId, CancellationToken cancellationToken)
    {
        using var context = contextFactory.CreateDbContext();
        var snapshot = await context.Orders.AsNoTracking().AsSplitQuery()
            .Where(order => order.Id == orderId)
            .Select(order => new
            {
                order.Id,
                order.OrderNumber,
                order.ModifiedAt,
                order.ClientId,
                order.DeliveryMethod,
                order.DeliverymanId,
                order.ShippingCarrierId,
                order.PaymentMethodId,
                order.OrderSource,
                order.Version,
                order.OrderStatus,
                order.DeliveryCost,
                order.DiscountType,
                order.DiscountValue,
                order.DiscountAmount,
                Items = order.OrderItems.Select(item => new
                {
                    item.Id,
                    item.ProductId,
                    item.ProductName,
                    CategoryName = item.Product.Category.Name.Value,
                    UnitPrice = item.UnitPrice.Value,
                    item.Quantity,
                    item.SupplierId,
                    item.SupplierName,
                    item.VatRate,
                    Suppliers = item.Product.Suppliers.Select(supplier => new OrderEditorSupplier(
                        supplier.Id, supplier.Name.Value)).ToList(),
                    CurrentProperties = item.Product.Properties.Select(definition => new OrderEditorProperty(
                        definition.PropertyId,
                        definition.Property.Name,
                        definition.IsRequired,
                        definition.Property.PropertyType,
                        definition.Property.Options.Select(option => new OrderEditorOption(option.Id, option.Value)).ToList(),
                        null)).ToList(),
                    SavedProperties = item.Properties
                        .Select(saved => new OrderEditorProperty(
                            saved.PropertyId,
                            saved.Property.Name,
                            false,
                            saved.Property.PropertyType,
                            saved.Property.Options.Select(option => new OrderEditorOption(option.Id, option.Value)).ToList(),
                            saved.Value)).ToList()
                }).ToList(),
                DeliverySteps = order.DeliverySteps.OrderBy(step => step.StepOrder)
                    .Select(step => new OrderEditorDeliveryStep(step.StepOrder, step.DeliveryMethod,
                        step.DeliveryMethod == DeliveryMethod.DeliveryMan ? step.DeliverymanId
                            : step.DeliveryMethod == DeliveryMethod.ShippingCompany ? step.ShippingCarrierId : null, step.Id))
                    .ToList(),
                Allocations = order.Allocations
                    .Where(allocation => allocation.Status == OrderAllocationStatus.Active)
                    .Select(allocation => new
                    {
                        allocation.OrderItemId,
                        allocation.SourceKind,
                        allocation.Qty,
                        allocation.WarehouseId,
                        WarehouseName = allocation.Warehouse == null ? null : allocation.Warehouse.Name.Value,
                        allocation.SupplierId,
                        SupplierName = allocation.Supplier == null ? null : allocation.Supplier.Name.Value
                    })
                    .ToList()
            }).SingleOrDefaultAsync(cancellationToken);

        if (snapshot is null)
            return null;

        return new OrderEditorData(snapshot.Id, snapshot.ClientId, snapshot.DeliveryMethod,
            snapshot.DeliverymanId, snapshot.ShippingCarrierId, snapshot.PaymentMethodId,
            snapshot.Items.Select(item => new OrderEditorItem(item.ProductId, item.ProductName, item.CategoryName,
                item.UnitPrice, item.Quantity, item.SupplierId, item.SupplierName, item.Suppliers,
                item.CurrentProperties.Select(definition => definition with
                {
                    SelectedValue = item.SavedProperties.SingleOrDefault(saved => saved.PropertyId == definition.PropertyId)?.SelectedValue
                }).Concat(item.SavedProperties.Where(saved => !item.CurrentProperties.Any(definition => definition.PropertyId == saved.PropertyId)))
                    .ToList(), item.VatRate, item.Id,
                snapshot.Allocations.Where(allocation => allocation.OrderItemId == item.Id)
                    .Select(allocation => new OrderEditorAllocation(allocation.SourceKind, allocation.Qty,
                        allocation.WarehouseId, allocation.WarehouseName, allocation.SupplierId, allocation.SupplierName))
                    .ToList())).ToList(),
            snapshot.DeliverySteps, snapshot.OrderSource, snapshot.Version, snapshot.OrderStatus,
            snapshot.DeliveryCost, snapshot.DiscountType, snapshot.DiscountValue, snapshot.DiscountAmount,
            snapshot.OrderNumber, snapshot.ModifiedAt);
    }
}
