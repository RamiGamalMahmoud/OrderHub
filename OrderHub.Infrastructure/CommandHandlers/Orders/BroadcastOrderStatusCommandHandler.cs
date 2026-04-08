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
                .Include(o => o.EntitySequences)
                .Include(o => o.ShippingCarrier).ThenInclude(s => s.Phone)
                .Include(o => o.Deliveryman).ThenInclude(d => d.WhatsappGroup)
                .Include(o => o.DeliverySteps).ThenInclude(step => step.Deliveryman).ThenInclude(d => d.WhatsappGroup)
                .Include(o => o.DeliverySteps).ThenInclude(step => step.ShippingCarrier).ThenInclude(carrier => carrier.Phone)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Supplier).ThenInclude(s => s.Phone)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Supplier).ThenInclude(s => s.WhatsappGroup)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Attributes)
                .Include(o => o.Client).ThenInclude(c => c.Phone)
                .Include(o => o.Client).ThenInclude(c => c.Address).ThenInclude(a => a.City)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

            List<OutboxMessage> outboxMessages = new List<OutboxMessage>();

            if (request.RecipientType is null or RecipientType.Client)
            {
                outboxMessages.Add(NotifyClient(order));
            }

            if (request.RecipientType is null or RecipientType.Supplier)
            {
                Result<IEnumerable<OutboxMessage>> supplierMessagesResult = NotifySuppliers(order);
                if (!supplierMessagesResult.IsSuccess)
                {
                    return Result.Failure(supplierMessagesResult.ErrorMessage);
                }

                outboxMessages.AddRange(supplierMessagesResult.Value);
            }

            if (request.RecipientType is null or RecipientType.Deliveryman or RecipientType.ShippingCarrier)
            {
                Result<IEnumerable<OutboxMessage>> deliveryMessagesResult = NotifyDeliveryRecipients(order, request.RecipientType);
                if (!deliveryMessagesResult.IsSuccess)
                {
                    return Result.Failure(deliveryMessagesResult.ErrorMessage);
                }

                outboxMessages.AddRange(deliveryMessagesResult.Value);
            }

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
            return new OutboxMessage
            {
                Order = order,
                OrderId = order.Id,
                RecipientType = RecipientType.Client,
                Text = BuildClientMessage(order),
                Status = OutboxMessageStatus.Pending,
                Recipient = new ClientRecipient
                {
                    ClientId = order.Client.Id,
                    Name = order.Client.Name.Value,
                    PhoneNumber = order.Client.Phone.Number.FullNumber,
                }
            };
        }

        private Result<IEnumerable<OutboxMessage>> NotifySuppliers(Order order)
        {
            IEnumerable<IGrouping<Supplier, OrderItem>> suppliers = order.OrderItems.GroupBy(oi => oi.Supplier);
            List<OutboxMessage> outboxMessages = new List<OutboxMessage>();

            foreach (IGrouping<Supplier, OrderItem> supplier in suppliers)
            {
                if (supplier.Key is null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(supplier.Key.WhatsappGroup?.GroupLink))
                {
                    return Result<IEnumerable<OutboxMessage>>.Failure($"المورد {supplier.Key.Name.Value} ليس لديه رابط مجموعة واتساب.");
                }

                outboxMessages.Add(new OutboxMessage
                {
                    Order = order,
                    OrderId = order.Id,
                    RecipientType = RecipientType.Supplier,
                    Text = BuildSupplierMessage(order, supplier.Key.Id, supplier.ToList()),
                    Status = OutboxMessageStatus.Pending,
                    Recipient = new SupplierRecipient
                    {
                        SupplierId = supplier.Key.Id,
                        Name = supplier.Key.Name.Value,
                        PhoneNumber = supplier.Key.WhatsappGroup.GroupLink
                    }
                });
            }

            return Result<IEnumerable<OutboxMessage>>.Success(outboxMessages);
        }

        private OutboxMessage NotifyDeliveryman(Order order)
        {
            return new OutboxMessage
            {
                Order = order,
                OrderId = order.Id,
                RecipientType = RecipientType.Deliveryman,
                Text = BuildDeliveryMessage(order, order.Deliveryman.Id, order.Deliveryman.Name.Value),
                Status = OutboxMessageStatus.Pending,
                Recipient = new DeliverymanRecipient
                {
                    DeliveryManId = order.Deliveryman.Id,
                    Name = order.Deliveryman.Name.Value,
                    PhoneNumber = order.Deliveryman.WhatsappGroup.GroupLink
                }
            };
        }

        private OutboxMessage NotifyDeliveryman(Order order, Deliveryman deliveryman)
        {
            return new OutboxMessage
            {
                Order = order,
                OrderId = order.Id,
                RecipientType = RecipientType.Deliveryman,
                Text = BuildDeliveryMessage(order, deliveryman.Id, deliveryman.Name.Value),
                Status = OutboxMessageStatus.Pending,
                Recipient = new DeliverymanRecipient
                {
                    DeliveryManId = deliveryman.Id,
                    Name = deliveryman.Name.Value,
                    PhoneNumber = deliveryman.WhatsappGroup.GroupLink
                }
            };
        }

        private OutboxMessage NotifyShippingCarrier(Order order)
        {
            return new OutboxMessage
            {
                Order = order,
                OrderId = order.Id,
                RecipientType = RecipientType.ShippingCarrier,
                Text = BuildShippingCarrierMessage(order),
                Status = OutboxMessageStatus.Pending,
                Recipient = new ShippingCarrierRecipient
                {
                    ShippingCarrierId = order.ShippingCarrier.Id,
                    Name = order.ShippingCarrier.Name.Value,
                    PhoneNumber = order.ShippingCarrier.Phone.Number.FullNumber
                }
            };
        }

        private OutboxMessage NotifyShippingCarrier(Order order, ShippingCarrier shippingCarrier)
        {
            return new OutboxMessage
            {
                Order = order,
                OrderId = order.Id,
                RecipientType = RecipientType.ShippingCarrier,
                Text = BuildShippingCarrierMessage(order),
                Status = OutboxMessageStatus.Pending,
                Recipient = new ShippingCarrierRecipient
                {
                    ShippingCarrierId = shippingCarrier.Id,
                    Name = shippingCarrier.Name.Value,
                    PhoneNumber = shippingCarrier.Phone.Number.FullNumber
                }
            };
        }

        private Result<IEnumerable<OutboxMessage>> NotifyDeliveryRecipients(Order order, RecipientType? recipientType = null)
        {
            if (TryGetMissingDeliverymanGroupName(order, recipientType, out string deliverymanName))
            {
                return Result<IEnumerable<OutboxMessage>>.Failure($"مندوب التوصيل {deliverymanName} ليس لديه رابط مجموعة واتساب.");
            }

            if (order.DeliveryMethod == DeliveryMethod.DeliveryChain)
            {
                IEnumerable<OutboxMessage> messages = order.DeliverySteps
                    .OrderBy(step => step.StepOrder)
                    .GroupBy(step => new { step.DeliveryMethod, step.DeliverymanId, step.ShippingCarrierId })
                    .Select(group => group.First())
                    .Where(step =>
                        recipientType is null
                        || (recipientType == RecipientType.Deliveryman && step.DeliveryMethod == DeliveryMethod.DeliveryMan)
                        || (recipientType == RecipientType.ShippingCarrier && step.DeliveryMethod == DeliveryMethod.ShippingCompany))
                    .Select(step => step.DeliveryMethod switch
                    {
                        DeliveryMethod.DeliveryMan when step.Deliveryman is not null => NotifyDeliveryman(order, step.Deliveryman),
                        DeliveryMethod.ShippingCompany when step.ShippingCarrier is not null => NotifyShippingCarrier(order, step.ShippingCarrier),
                        _ => null
                    })
                    .Where(message => message is not null)
                    .ToList();

                return Result<IEnumerable<OutboxMessage>>.Success(messages);
            }

            List<OutboxMessage> outboxMessages = new List<OutboxMessage>();

            if (order.Deliveryman is not null && (recipientType is null or RecipientType.Deliveryman))
            {
                outboxMessages.Add(NotifyDeliveryman(order));
            }

            if (order.ShippingCarrier is not null && (recipientType is null or RecipientType.ShippingCarrier))
            {
                outboxMessages.Add(NotifyShippingCarrier(order));
            }

            return Result<IEnumerable<OutboxMessage>>.Success(outboxMessages);
        }

        private static bool TryGetMissingDeliverymanGroupName(Order order, RecipientType? recipientType, out string deliverymanName)
        {
            if (order.DeliveryMethod == DeliveryMethod.DeliveryChain)
            {
                Deliveryman missingDeliveryman = order.DeliverySteps
                    .Where(step => step.DeliveryMethod == DeliveryMethod.DeliveryMan)
                    .Select(step => step.Deliveryman)
                    .FirstOrDefault(deliveryman =>
                        deliveryman is not null
                        && (recipientType is null or RecipientType.Deliveryman)
                        && string.IsNullOrWhiteSpace(deliveryman.WhatsappGroup?.GroupLink));

                deliverymanName = missingDeliveryman?.Name.Value;
                return missingDeliveryman is not null;
            }

            if (recipientType is RecipientType.ShippingCarrier || order.Deliveryman is null)
            {
                deliverymanName = null;
                return false;
            }

            deliverymanName = order.Deliveryman.Name.Value;
            return (recipientType is null or RecipientType.Deliveryman)
                && string.IsNullOrWhiteSpace(order.Deliveryman.WhatsappGroup?.GroupLink);
        }

        private string BuildClientMessage(Order order)
        {
            StringBuilder sb = CreateMessageHeader(
                order.GetDisplayTitle(RecipientType.Client, order.ClientId),
                order.OrderNumber,
                order);

            sb.AppendLine("-----------------------------");
            sb.AppendLine("*المنتجات*");
            sb.AppendLine();

            int index = 1;
            foreach (OrderItem item in order.OrderItems)
            {
                AppendProductBlock(sb, index, item, includePrice: true);
                index++;
            }

            sb.AppendLine("----------------------");
            sb.AppendLine($"الإجمالي: {order.Total.Value:0.00}");
            sb.AppendLine();
            sb.AppendLine("----------------------");
            sb.AppendLine("> ملاحظة : ");

            return sb.ToString();
        }

        private string BuildSupplierMessage(Order order, int supplierId, IReadOnlyCollection<OrderItem> items)
        {
            StringBuilder sb = CreateMessageHeader(
                order.GetDisplayTitle(RecipientType.Supplier, supplierId),
                order.OrderNumber,
                order);

            sb.AppendLine();
            sb.AppendLine("-----------------------------");
            sb.AppendLine("*المنتجات*");
            sb.AppendLine();

            int index = 1;
            foreach (OrderItem item in items)
            {
                AppendProductBlock(sb, index, item, includePrice: false);
                index++;
            }

            sb.AppendLine("-----------------------------");
            sb.AppendLine("> ملاحظة : ");

            return sb.ToString();
        }

        private string BuildDeliveryMessage(Order order, int deliverymanId, string deliverymanName)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"*{order.GetDisplayTitle(RecipientType.Deliveryman, deliverymanId)}*");
            sb.AppendLine();
            sb.Append(BuildBaseDetails(order));
            sb.AppendLine($"*اسم المندوب:* {deliverymanName}");
            sb.AppendLine("-----------------------------");
            sb.AppendLine("*المنتجات*");
            sb.AppendLine();

            int index = 1;
            foreach (OrderItem item in order.OrderItems)
            {
                AppendProductBlock(sb, index, item, includePrice: false);
                index++;
            }

            sb.AppendLine("----------------------");
            sb.AppendLine(" ");
            sb.AppendLine("> ملاحظة : ");

            return sb.ToString();
        }

        private string BuildShippingCarrierMessage(Order order)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"*{order.GetDisplayTitle(RecipientType.Client, order.ClientId)}*");
            sb.AppendLine();
            sb.Append(BuildBaseDetails(order));
            sb.AppendLine("-----------------------------");
            sb.AppendLine("*المنتجات*");
            sb.AppendLine();

            int index = 1;
            foreach (OrderItem item in order.OrderItems)
            {
                AppendProductBlock(sb, index, item, includePrice: false);
                index++;
            }

            sb.AppendLine("----------------------");
            sb.AppendLine(" ");
            sb.AppendLine("> ملاحظة : ");

            return sb.ToString();
        }

        private StringBuilder CreateMessageHeader(string title, string orderNumber, Order order)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"*{title}*");
            sb.AppendLine();
            sb.Append(BuildBaseDetails(order, orderNumber));
            return sb;
        }

        private string BuildBaseDetails(Order order, string orderNumber = null)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"*رقم الطلب:* {orderNumber ?? order.OrderNumber}");
            sb.AppendLine($"*التاريخ:* {order.CreatedAt:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"*العميل:* {order.Client.Name.Value}");
            sb.AppendLine($"*رقم العميل:* {order.Client.Phone.Number.FullNumber}");
            sb.AppendLine($"*العنوان:* {order.Client.Address.FullAddress}");
            sb.AppendLine("*الموقع الخريطه*");
            return sb.ToString();
        }

        private static void AppendProductBlock(StringBuilder sb, int index, OrderItem item, bool includePrice)
        {
            sb.AppendLine($"{index}) {item.ProductName}");
            sb.AppendLine($"الكمية: {item.Quantity}");

            foreach (OrderItemAttribute attribute in item.Attributes)
            {
                sb.AppendLine($"{attribute.Name} : {attribute.Value}");
            }

            if (includePrice)
            {
                sb.AppendLine($"السعر: {item.UnitPrice.Value:0.00}");
            }

            sb.AppendLine();
        }
    }
}
