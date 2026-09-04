using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Orders.NotifyOrderParticipants;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Queries.Orders;

internal class OrderQueries : IOrderNotificationQuery
{
    private readonly AppDbContext _context;

    public OrderQueries(AppDbContext context)
    {
        _context = context;
    }

    public async Task<OrderNotification> GetForNotificationAsync(
    int orderId,
    CancellationToken cancellationToken)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => new OrderNotification
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CreatedAt = o.CreatedAt,
                DeliveryMethod = o.DeliveryMethod,

                Total = o.OrderItems
                    .Sum(i => i.UnitPrice.Value * i.Quantity),

                Client = new ClientNotification
                {
                    Id = o.ClientId,
                    Name = o.Client.Name.Value,
                    Phone = o.Client.Phone.Number.FullNumber,

                    Address = o.Client.Address.City.Name.Value
                             + " - "
                             + o.Client.Address.Street,

                    Location = o.Client.Location
                },

                Items = o.OrderItems
                    .Select(i => new OrderItemNotification
                    {
                        ProductName = i.ProductName,
                        UnitPrice = i.UnitPrice.Value,
                        Quantity = i.Quantity,

                        Supplier = i.Supplier == null
                            ? null
                            : new SupplierNotification
                            {
                                Id = i.Supplier.Id,
                                Name = i.Supplier.Name.Value,
                                WhatsappGroupLink =
                                    i.Supplier.WhatsappGroup.GroupLink
                            }
                    })
                    .ToList(),

                Deliveryman = o.Deliveryman == null
                    ? null
                    : new DeliverymanNotification
                    {
                        Id = o.Deliveryman.Id,
                        Name = o.Deliveryman.Name.Value,
                        Phone = o.Deliveryman.PhoneNumber,
                        City = o.Deliveryman.City.Name.Value,
                        WhatsappGroupLink =
                            o.Deliveryman.WhatsappGroup.GroupLink
                    },

                ShippingCarrier = o.ShippingCarrier == null
                    ? null
                    : new ShippingCarrierNotification
                    {
                        Id = o.ShippingCarrier.Id,
                        Name = o.ShippingCarrier.Name.Value,
                        Phone = o.ShippingCarrier.Phone.Number.FullNumber,

                        Address =
                            o.ShippingCarrier.Address.City.Name.Value
                            + " - "
                            + o.ShippingCarrier.Address.Street
                    },

                DeliverySteps = o.DeliverySteps
                    .OrderBy(s => s.StepOrder)
                    .Select(s => new DeliveryStepNotification
                    {
                        StepOrder = s.StepOrder,
                        DeliveryMethod = s.DeliveryMethod,

                        Deliveryman = s.Deliveryman == null
                            ? null
                            : new DeliverymanNotification
                            {
                                Id = s.Deliveryman.Id,
                                Name = s.Deliveryman.Name.Value,
                                Phone = s.Deliveryman.PhoneNumber,
                                City = s.Deliveryman.City.Name.Value,
                                WhatsappGroupLink =
                                    s.Deliveryman.WhatsappGroup.GroupLink
                            },

                        ShippingCarrier = s.ShippingCarrier == null
                            ? null
                            : new ShippingCarrierNotification
                            {
                                Id = s.ShippingCarrier.Id,
                                Name = s.ShippingCarrier.Name.Value,
                                Phone = s.ShippingCarrier.Phone.Number.FullNumber,

                                Address =
                                    s.ShippingCarrier.Address.City.Name.Value
                                    + " - "
                                    + s.ShippingCarrier.Address.Street
                            }
                    })
                    .ToList(),

                EntitySequences = o.EntitySequences
                    .Select(s => new OrderEntitySequenceNotification
                    {
                        RecipientType = s.RecipientType,
                        EntityId = s.EntityId,
                        DisplayTitle = s.DisplayTitle
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);
    }
}
