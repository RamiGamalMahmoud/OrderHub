using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Text;

namespace OrderHub.Infrastructure.CommandHandlers.Orders.MessageBulders;

internal abstract class MessageBuilderBase
{
    protected virtual void CreateMessageHeader(StringBuilder sb, string title, string orderNumber, Order order)
    {
        sb.AppendLine($"*{title}*");
        sb.AppendLine();
        BuildBaseDetails(sb, order, orderNumber);
    }

    protected virtual void AppendProductBlock(StringBuilder sb, int index, OrderItem item, bool includePrice)
    {
        sb.AppendLine($"{index}) {item.ProductName}");
        sb.AppendLine($"الكمية: {item.Quantity}");

        if (includePrice)
        {
            sb.AppendLine($"السعر: {item.UnitPrice.Value:0.00}");
        }

        sb.AppendLine();
    }

    protected virtual void BuildBaseDetails(StringBuilder sb, Order order, string orderNumber = null)
    {
        sb.AppendLine($"*رقم الطلب:* {orderNumber ?? order.OrderNumber}");
        sb.AppendLine($"*التاريخ:* {order.CreatedAt:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"*العميل:* {order.Client.Name.Value}");
        sb.AppendLine($"*رقم العميل:* {order.Client.Phone.Number.FullNumber}");
        sb.AppendLine($"*العنوان:* {order.Client.Address.FullAddress}");
        sb.AppendLine("*الموقع الخريطه:*");
        sb.AppendLine(order.Client.Location);
    }

    protected virtual void AppendFooter(StringBuilder sb)
    {
        sb.AppendLine("----------------------");
    }

protected static void AppendDeliveryDestination(
    StringBuilder sb,
    Order order,
    OrderDeliveryStep nextStep)
    {
        sb.AppendLine();

        if (nextStep is null)
        {
            sb.AppendLine("*نوع المستلم:* العميل");
            sb.AppendLine($"*الاسم:* {order.Client.Name.Value}");
            sb.AppendLine($"*الهاتف:* {order.Client.Phone.Number.FullNumber}");
            sb.AppendLine($"*العنوان:* {order.Client.Address.FullAddress}");

            return;
        }

        switch (nextStep.DeliveryMethod)
        {
            case DeliveryMethod.ShippingCompany:
                {
                    ShippingCarrier carrier = nextStep.ShippingCarrier;

                    sb.AppendLine("*نوع المستلم:* شركة شحن");
                    sb.AppendLine($"*الاسم:* {carrier.Name.Value}");
                    sb.AppendLine($"*الهاتف:* {carrier.Phone}");
                    sb.AppendLine($"*العنوان:* {carrier.Address.City.Name.Value} - {carrier.Address.Street}");

                    break;
                }

            case DeliveryMethod.DeliveryMan:
                {
                    Deliveryman deliveryman = nextStep.Deliveryman;

                    sb.AppendLine("*نوع المستلم:* مندوب توصيل");
                    sb.AppendLine($"*الاسم:* {deliveryman.Name.Value}");
                    sb.AppendLine($"*الهاتف:* {deliveryman.PhoneNumber}");
                    sb.AppendLine($"*العنوان:* {deliveryman.City.Name.Value}");

                    break;
                }

            default:
                throw new InvalidOperationException(
                    $"Unsupported delivery method: {nextStep.DeliveryMethod}");
        }
    }
}
