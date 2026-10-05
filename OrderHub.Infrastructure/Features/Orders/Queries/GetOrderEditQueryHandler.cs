using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Orders.Queries;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Features.Orders.Queries;
internal class GetOrderEditQueryHandler(
    AppDbContextFactory contextFactory)
    : IRequestHandler<GetOrderEdit.Query, GetOrderEdit.Order>
{
    public async Task<GetOrderEdit.Order> Handle(
        GetOrderEdit.Query request,
        CancellationToken cancellationToken)
    {
        using AppDbContext dbContext = contextFactory.CreateDbContext();

        Order order = await dbContext.Orders
            .AsNoTracking()
            .AsSplitQuery()

            .Include(o => o.OrderItems)
                .ThenInclude(item => item.Product)
                    .ThenInclude(product => product.Category)

            .Include(o => o.OrderItems)
                .ThenInclude(item => item.Product)
                    .ThenInclude(product => product.Suppliers)

            .Include(o => o.OrderItems)
                .ThenInclude(item => item.Product)
                    .ThenInclude(product => product.Properties)
                        .ThenInclude(productProperty => productProperty.Property)
                            .ThenInclude(property => property.Options)

            .Include(o => o.OrderItems)
                .ThenInclude(item => item.Properties)
                    .ThenInclude(itemProperty => itemProperty.Property)
                        .ThenInclude(property => property.Options)

            .Include(o => o.DeliverySteps)

            .SingleOrDefaultAsync(
                o => o.Id == request.OrderId,
                cancellationToken);

        if (order is null)
            return null;

        return MapOrder(order);
    }

    internal static GetOrderEdit.Order MapOrder(Order order)
    {
        return new GetOrderEdit.Order(
            order.Id,
            order.ClientId,
            order.DeliveryMethod,
            order.DeliverymanId,
            order.ShippingCarrierId,
            order.PaymentMethodId,

            order.OrderItems.Select(orderItem =>
                new GetOrderEdit.OrderItem(
                    orderItem.ProductId,
                    orderItem.ProductName,
                    orderItem.Product.Category.Name.Value,
                    orderItem.UnitPrice.Value,
                    orderItem.Quantity,
                    orderItem.SupplierId,
                    orderItem.SupplierName,

                    orderItem.Product.Suppliers.Select(supplier =>
                        new GetOrderEdit.Supplier(
                            supplier.Id,
                            supplier.Name.Value)),

                    orderItem.Product.Properties.Select(productProperty =>
                    {
                        var orderItemProperty = orderItem.Properties
                            .SingleOrDefault(x =>
                                x.PropertyId == productProperty.PropertyId);

                        return new GetOrderEdit.Property(
                            productProperty.PropertyId,
                            productProperty.Property.Name,
                            productProperty.IsRequired,
                            productProperty.Property.PropertyType,

                            productProperty.Property.Options.Select(option =>
                                new GetOrderEdit.Option(
                                    option.Id,
                                    option.Value)),

                            orderItemProperty?.Value);
                    }).Concat(orderItem.Properties
                        .Where(saved => !orderItem.Product.Properties.Any(current => current.PropertyId == saved.PropertyId))
                        .Select(saved => new GetOrderEdit.Property(
                            saved.PropertyId,
                            saved.Property.Name,
                            // There is no current product requirement for this removed association.
                            // Its historical IsRequired flag is not persisted on the order.
                            false,
                            saved.Property.PropertyType,
                            saved.Property.Options.Select(option => new GetOrderEdit.Option(option.Id, option.Value)),
                            saved.Value))).ToList(),
                    orderItem.VatRate
                )).ToList(),

            order.DeliverySteps.OrderBy(step => step.StepOrder).Select(deliveryStep =>
                new GetOrderEdit.DeliveryStep(
                    deliveryStep.StepOrder,
                    deliveryStep.DeliveryMethod,
                    deliveryStep.DeliveryMethod switch
                    {
                        DeliveryMethod.DeliveryMan => deliveryStep.DeliverymanId,
                        DeliveryMethod.ShippingCompany => deliveryStep.ShippingCarrierId,
                        _ => null
                    })).ToList(),
            order.OrderSource
        );
    }
}
