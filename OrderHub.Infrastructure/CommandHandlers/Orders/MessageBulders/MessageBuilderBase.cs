using OrderHub.Domain.Models;
using System.Text;

namespace OrderHub.Infrastructure.CommandHandlers.Orders.MessageBulders;

internal abstract class MessageBuilderBase
{
    protected readonly StringBuilder _sb = new StringBuilder();
    
    protected virtual void CreateMessageHeader(string title, string orderNumber, Order order)
    {
        _sb.Clear();
        _sb.AppendLine($"*{title}*");
        _sb.AppendLine();
        BuildBaseDetails(order, orderNumber);
    }

    protected virtual void AppendProductBlock(int index, OrderItem item, bool includePrice)
    {
        _sb.AppendLine($"{index}) {item.ProductName}");
        _sb.AppendLine($"الكمية: {item.Quantity}");

        foreach (OrderItemAttribute attribute in item.Attributes)
        {
            _sb.AppendLine($"{attribute.Name} : {attribute.Value}");
        }

        if (includePrice)
        {
            _sb.AppendLine($"السعر: {item.UnitPrice.Value:0.00}");
        }

        _sb.AppendLine();
    }

    protected virtual void BuildBaseDetails(Order order, string orderNumber = null)
    {
        _sb.AppendLine($"*رقم الطلب:* {orderNumber ?? order.OrderNumber}");
        _sb.AppendLine($"*التاريخ:* {order.CreatedAt:yyyy-MM-dd HH:mm}");
        _sb.AppendLine($"*العميل:* {order.Client.Name.Value}");
        _sb.AppendLine($"*رقم العميل:* {order.Client.Phone.Number.FullNumber}");
        _sb.AppendLine($"*العنوان:* {order.Client.Address.FullAddress}");
        _sb.AppendLine("*الموقع الخريطه*");
    }

    protected virtual void AppendFooter()
    {
        _sb.AppendLine("----------------------");
        _sb.AppendLine(" ");
        _sb.AppendLine("> ملاحظة : ");
    }
}
