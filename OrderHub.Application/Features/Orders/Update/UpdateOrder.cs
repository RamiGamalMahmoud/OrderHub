using MediatR;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.Update;

public static class UpdateOrder
{
    public record Command(int OrderId, OrderDetails.Order Order) : IRequest<Result>;

    internal class Handler : IRequestHandler<Command, Result>
    {
        private readonly IOrderStore _orderStore;

        public Handler(IOrderStore orderStore)
        {
            _orderStore = orderStore;
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            return await _orderStore.UpdateOrder(request.OrderId, request.Order, cancellationToken);
        }
    }

    public record OrderEdit(
    int Id,
    int ClientId,
    DeliveryMethod DeliveryMethod,
    int? DeliveryManId,
    int? ShippingCarrierId,
    int? PaymentMothodId,
    IEnumerable<OrderItem> OrderItems,
    IEnumerable<DeliveryStep> DeliverySteps);

    public record OrderItem(
    int ProductId,
    string ProductName,
    string CategoryName,
    decimal UnitPrice,
    int Quantity,
    int? SupplierId,
    string SupplierName,
    IEnumerable<Supplier> Suppliers);

    public record DeliveryStep(
        int StepOrder,
        DeliveryMethod DeliveryMethod,
        int HandlerId);

    public record Supplier(int Id, string Name);
}
