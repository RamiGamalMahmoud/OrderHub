using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Models;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.OrderDtos;
using static OrderHub.Application.Queries.OrderQueries;

namespace OrderHub.Infrastructure.QueryHandlers.Orders;

internal class GetOrderForEditQueryHandler(AppDbContextFactory appDbContextFactory)
    : IRequestHandler<GetOrderForEditQuery, OrderEditDto>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<OrderEditDto> Handle(GetOrderForEditQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        Order order = await appDbContext.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
                .ThenInclude(item => item.Attributes)
            .Include(o => o.OrderItems)
                .ThenInclude(item => item.Product)
                    .ThenInclude(product => product.Category)
            .Include(o => o.OrderItems)
                .ThenInclude(item => item.Product)
                    .ThenInclude(product => product.Suppliers)
            .Include(o => o.DeliverySteps)
            .SingleOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            return null;
        }

        return new OrderEditDto(
            order.Id,
            order.ClientId,
            order.DeliveryMethod ?? Domain.Enums.DeliveryMethod.Pickup,
            order.DeliverymanId,
            order.ShippingCarrierId,
            order.PaymentMethodId,
            order.OrderItems
                .Select(item => new OrderItemEditDto(
                    item.ProductId,
                    item.ProductName,
                    item.Product?.Category?.Name?.Value ?? string.Empty,
                    item.UnitPrice.Value,
                    item.Quantity,
                    item.SupplierId,
                    item.SupplierName,
                    item.Product?.Suppliers
                        .Select(supplier => new Application.DTOs.CommonDtos.SupplierInfoDto(supplier.Id, supplier.Name.Value))
                        .ToImmutableList()
                    ?? ImmutableList<Application.DTOs.CommonDtos.SupplierInfoDto>.Empty,
                    item.Attributes
                        .Select(attribute => new OrderItemAttributeDto(attribute.Name, attribute.Value))
                        .ToImmutableList()))
                .ToImmutableList(),
            order.DeliverySteps
                .OrderBy(step => step.StepOrder)
                .Select(step => new OrderDeliveryStepEditDto(
                    step.StepOrder,
                    step.DeliveryMethod,
                    step.DeliveryMethod == Domain.Enums.DeliveryMethod.DeliveryMan
                        ? step.DeliverymanId ?? 0
                        : step.ShippingCarrierId ?? 0))
                .ToImmutableList());
    }
}
