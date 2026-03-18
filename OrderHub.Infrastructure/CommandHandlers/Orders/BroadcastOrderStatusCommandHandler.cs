using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.OrderCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Orders
{
    internal class BroadcastOrderStatusCommandHandler(AppDbContextFactory appDbContextFactory, IMessenger messenger) : IRequestHandler<BroadcastOrderStatusCommand, Result>
    {
        private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

        public async Task<Result> Handle(BroadcastOrderStatusCommand request, CancellationToken cancellationToken)
        {
            using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
            Order order = await appDbContext
                .Orders
                .Include(o => o.ShippingCarrier).ThenInclude(s => s.Phone)
                .Include(o => o.Deliveryman)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Supplier).ThenInclude(s => s.Phone)
                .Include(o => o.Client).ThenInclude(c => c.Phone)
                .Include(o => o.Client).ThenInclude(c => c.Address).ThenInclude(a => a.City)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

            List<OutboxMessage> outboxMessages = new List<OutboxMessage>();

            outboxMessages.Add(NotifyClient(order));
            outboxMessages.AddRange(NotifSuppliers(order));
            if (order.Deliveryman is not null)
                outboxMessages.Add(NotifyDeliveryman(order));

            if (order.ShippingCarrier is not null)
                outboxMessages.Add(NotifyShippingCarrier(order));

            appDbContext.OutboxMessages.AddRange(outboxMessages);
            try
            {
                await appDbContext.SaveChangesAsync(cancellationToken);
                messenger.Send(new Application.Messages.Orders.MessagesCreatedMessage(outboxMessages));
                return Result.Success();
            }
            catch (Exception)
            {
                return Result.Failure("Failed to broadcast order status");
            }
        }

        private OutboxMessage NotifyClient(Order order)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("طلب جديد");
            sb.AppendLine("----------------------");

            sb.AppendLine($"التاريخ: {order.CreatedAt}");
            sb.AppendLine($"رقم الطلب: {order.OrderNumber}");
            sb.AppendLine();

            sb.AppendLine($"العميل: {order.Client.Name.Value}");
            sb.AppendLine($"رقم الهاتف: {order.Client.Phone.Number.FullNumber}");
            sb.AppendLine();

            sb.AppendLine("----------------------");
            sb.AppendLine("المنتجات");
            sb.AppendLine();

            int i = 1;

            foreach (var item in order.OrderItems)
            {
                sb.AppendLine($"{i}) {item.ProductName}");
                sb.AppendLine($"الكمية: {item.Quantity}");
                sb.AppendLine($"السعر: {item.UnitPrice}");
                sb.AppendLine();
                i++;
            }

            sb.AppendLine("----------------------");
            sb.AppendLine($"الإجمالي: {order.Total}");

            string message = sb.ToString();

            OutboxMessage outboxMessage = new OutboxMessage()
            {
                OrderId = order.Id,
                RecipientType = RecipientType.Client,
                Text = message,
                Status = OutboxMessageStatus.Pending,
                Recipient = new ClientRecipient()
                {
                    ClientId = order.Client.Id,
                    Name = order.Client.Name.Value,
                    PhoneNumber = order.Client.Phone.Number.FullNumber,
                },

            };
            return outboxMessage;
        }

        private IEnumerable<OutboxMessage> NotifSuppliers(Order order)
        {
            var suppliers = order.OrderItems
                .GroupBy(oi => oi.Supplier);
            List<OutboxMessage> outboxMessages = new List<OutboxMessage>();
            foreach (var supplier in suppliers)
            {
                if (supplier.Key is null)
                    continue;
                IEnumerable<OrderItem> items = supplier.ToList();

                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"*طلب: {order.CreatedAt:yyyy-MM-dd}*");
                sb.AppendLine($"*رقم الطلب : {order.OrderNumber}*");
                sb.AppendLine($"*رقم العميل : {order.Client.Phone.Number.FullNumber}*");
                sb.AppendLine($"*العميل : {order.Client.Name.Value}*");
                sb.AppendLine($"*العنوان : {order.Client.Address.FullAddress}*");

                sb.AppendLine("-----------------------------");
                sb.AppendLine("*المنتجات*");
                sb.AppendLine("-----------------------------");

                int i = 1;

                foreach (var item in items)
                {
                    decimal subtotal = item.UnitPrice.Value * item.Quantity;

                    sb.AppendLine($"{i}- {item.ProductName}");
                    sb.AppendLine($"الكمية : {item.Quantity}");
                    sb.AppendLine($"السعر  : {subtotal:0.00}");
                    sb.AppendLine("");

                    i++;
                }

                sb.AppendLine("-----------------------------");

                sb.AppendLine($"*عدد المنتجات : {items.Count()}*");
                sb.AppendLine($"*الإجمالي : {items.Sum(i => i.UnitPrice.Value * i.Quantity):0.00}*");

                string messsage = sb.ToString();
                outboxMessages.Add(new OutboxMessage()
                {
                    Text = messsage,
                    OrderId = order.Id,
                    RecipientType = RecipientType.Supplier,
                    Status = OutboxMessageStatus.Pending,
                    Recipient = new SupplierRecipient()
                    {
                        SupplierId = supplier.Key.Id,
                        Name = supplier.Key.Name.Value,
                        PhoneNumber = supplier.Key.Phone.Number.FullNumber
                    }
                });
            }
            return outboxMessages;
        }

        private OutboxMessage NotifyDeliveryman(Order order)
        {
            OutboxMessage outboxMessage = new OutboxMessage()
            {
                OrderId = order.Id,
                RecipientType = RecipientType.Deliveryman,
                Text = "",
                Status = OutboxMessageStatus.Pending,
                Recipient = new DeliverymanRecipient()
                {
                    DeliveryManId = order.Deliveryman.Id,
                    Name = order.Deliveryman.Name.Value,
                    PhoneNumber = order.Deliveryman.PhoneNumber
                }
            };

            return outboxMessage;
        }

        private OutboxMessage NotifyShippingCarrier(Order order)
        {
            OutboxMessage outboxMessage = new OutboxMessage()
            {
                OrderId = order.Id,
                RecipientType = RecipientType.ShippingCarrier,
                Text = "",
                Status = OutboxMessageStatus.Pending,
                Recipient = new ShippingCarrierRecipient()
                {
                    ShippingCarrierId = order.ShippingCarrier.Id,
                    Name= order.ShippingCarrier.Name.Value,
                    PhoneNumber = order.ShippingCarrier.Phone.Number.FullNumber
                }
            };
            return outboxMessage;
        }
    }
}
