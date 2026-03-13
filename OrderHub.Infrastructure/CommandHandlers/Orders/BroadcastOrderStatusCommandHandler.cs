using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Models;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.OrderCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Orders
{
    internal class BroadcastOrderStatusCommandHandler(AppDbContextFactory appDbContextFactory, IWhatsappService whatsappService) : IRequestHandler<BroadcastOrderStatusCommand>
    {
        private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;
        private readonly IWhatsappService _whatsappService = whatsappService;

        public async Task Handle(BroadcastOrderStatusCommand request, CancellationToken cancellationToken)
        {
            using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
            Order order = await appDbContext
                .Orders
                .Include(o => o.ShippingCarrier).ThenInclude(s => s.Phone)
                .Include(o => o.Deliveryman)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Supplier).ThenInclude(s => s.Phone)
                .Include(o => o.Client).ThenInclude(c => c.Phone)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

            await NotifyClient(order.Id, appDbContext);
            //await NotifSuppliers(order.Id, appDbContext);
            //await NotifyDeliveryman(order.Id, appDbContext);
            //await NotifyShippingCarrier(order.Id, appDbContext);
        }

        private async Task NotifyClient(int orderId, AppDbContext appDbContext)
        {
            var clientData = await appDbContext.Orders
                .AsNoTracking()
                .Where(o => o.Id == orderId)
                .Select(o => new
                {
                    ClientPhone = o.Client.Phone.Number.FullNumber,
                    ClientName = o.Client.Name.Value,
                    OrderNumber = o.OrderNumber,
                    CreationData = o.CreatedAt.ToString("yyyy-MM-dd"),
                    OrderItems = o.OrderItems,
                    Total = o.OrderItems.Sum(oi => oi.UnitPrice.Value * oi.Quantity),
                    ItemsCount = o.OrderItems.Count
                })
                .FirstOrDefaultAsync();
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("طلب جديد");
            sb.AppendLine("----------------------");

            sb.AppendLine($"التاريخ: {clientData.CreationData}");
            sb.AppendLine($"رقم الطلب: {clientData.OrderNumber}");
            sb.AppendLine();

            sb.AppendLine($"العميل: {clientData.ClientName}");
            sb.AppendLine($"رقم الهاتف: {clientData.ClientPhone}");
            sb.AppendLine();

            sb.AppendLine("----------------------");
            sb.AppendLine("المنتجات");
            sb.AppendLine();

            int i = 1;

            foreach (var item in clientData.OrderItems)
            {
                sb.AppendLine($"{i}) {item.ProductName}");
                sb.AppendLine($"الكمية: {item.Quantity}");
                sb.AppendLine($"السعر: {item.UnitPrice}");
                sb.AppendLine();
                i++;
            }

            sb.AppendLine("----------------------");
            sb.AppendLine($"الإجمالي: {clientData.Total}");

            string message = sb.ToString();
            await _whatsappService.Send(clientData.ClientPhone, message);
        }

        private async Task NotifSuppliers(int orderId, AppDbContext appDbContext)
        {
            Order order = await appDbContext.Orders
                .Where(o => o.Id == orderId)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Supplier)
                        .ThenInclude(s => s.Phone)
                .Include(o => o.Client).ThenInclude(c => c.Phone)
                .Include(o => o.Client).ThenInclude(c => c.Address).ThenInclude(a => a.City)
                .FirstOrDefaultAsync();

            var sb = new StringBuilder();

            sb.AppendLine($"*طلب: {order.CreatedAt:yyyy-MM-dd}*");
            sb.AppendLine($"*رقم الطلب : {order.OrderNumber}*");
            sb.AppendLine($"*رقم العميل : {order.Client.Phone.Number.FullNumber}*");
            sb.AppendLine($"*العميل : {order.Client.Name.Value}*");
            sb.AppendLine($"*العنوان : {order.Client.Address.FullAddress}*");

            sb.AppendLine("-----------------------------");
            sb.AppendLine("*المنتجات*");
            sb.AppendLine("-----------------------------");

            int i = 1;

            foreach (var item in order.OrderItems)
            {
                decimal subtotal = item.UnitPrice.Value * item.Quantity;

                sb.AppendLine($"{i}- {item.ProductName}");
                sb.AppendLine($"الكمية : {item.Quantity}");
                sb.AppendLine($"السعر  : {subtotal:0.00}");
                sb.AppendLine("");

                i++;
            }

            sb.AppendLine("-----------------------------");

            sb.AppendLine($"*عدد المنتجات : {order.OrderItems.Count}*");
            sb.AppendLine($"*الإجمالي : {order.OrderItems.Sum(i => i.UnitPrice.Value * i.Quantity):0.00}*");

            string message = sb.ToString();
            await _whatsappService.Send("+201116040634", message);
        }

        private Task NotifyDeliveryman(int orderId, AppDbContext appDbContext)
        {
            return Task.CompletedTask;
        }

        private Task NotifyShippingCarrier(int orderId, AppDbContext appDbContext)
        {
            return Task.CompletedTask;
        }
    }
}
