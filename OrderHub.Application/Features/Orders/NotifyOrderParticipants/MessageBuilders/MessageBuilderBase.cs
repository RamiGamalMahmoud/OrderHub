using OrderHub.Application.Features.Orders.NotifyOrderParticipants;
using OrderHub.Domain.Enums;
using System;
using System.Linq;
using System.Text;

namespace OrderHub.Application.Features.Orders.NotifyOrderParticipants.MessageBuilders;

internal abstract class MessageBuilderBase
{
    protected static void CreateMessageHeader(
        StringBuilder sb,
        string title,
        OrderNotification order)
    {
        sb.AppendLine($"*{title}*");
        sb.AppendLine();

        BuildBaseDetails(sb, order);
    }

    protected static void BuildBaseDetails(
        StringBuilder sb,
        OrderNotification order)
    {
        sb.AppendLine($"*رقم الطلب:* {order.OrderNumber}");
        sb.AppendLine($"*التاريخ:* {order.CreatedAt:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"*العميل:* {order.Client.Name}");
        sb.AppendLine($"*رقم العميل:* {order.Client.Phone}");
        sb.AppendLine($"*العنوان:* {order.Client.Address}");
        sb.AppendLine("*الموقع الخريطة:*");
        sb.AppendLine(order.Client.Location);
    }

    protected static void AppendProducts(
        StringBuilder sb,
        OrderNotification order,
        bool includePrice)
    {
        sb.AppendLine("-----------------------------");
        sb.AppendLine("*المنتجات*");
        sb.AppendLine();

        int index = 1;

        foreach (OrderItemNotification item in order.Items)
        {
            AppendProductBlock(
                sb,
                index,
                item,
                includePrice);

            index++;
        }
    }

    protected static void AppendProductBlock(
        StringBuilder sb,
        int index,
        OrderItemNotification item,
        bool includePrice)
    {
        sb.AppendLine($"{index}) {item.ProductName}");
        sb.AppendLine($"الكمية: {item.Quantity}");

        if (includePrice)
        {
            sb.AppendLine($"السعر: {item.UnitPrice:0.00}");
        }

        sb.AppendLine();
    }

    protected static void AppendDeliveryDestination(
        StringBuilder sb,
        OrderNotification order,
        DeliveryStepNotification nextStep)
    {
        sb.AppendLine();

        if (nextStep is null)
        {
            sb.AppendLine("*نوع المستلم:* العميل");
            sb.AppendLine($"*الاسم:* {order.Client.Name}");
            sb.AppendLine($"*الهاتف:* {order.Client.Phone}");
            sb.AppendLine($"*العنوان:* {order.Client.Address}");

            return;
        }

        switch (nextStep.DeliveryMethod)
        {
            case DeliveryMethod.ShippingCompany:
                {
                    ShippingCarrierNotification carrier =
                        nextStep.ShippingCarrier;

                    sb.AppendLine("*نوع المستلم:* شركة شحن");
                    sb.AppendLine($"*الاسم:* {carrier.Name}");
                    sb.AppendLine($"*الهاتف:* {carrier.Phone}");
                    sb.AppendLine($"*العنوان:* {carrier.Address}");

                    break;
                }

            case DeliveryMethod.DeliveryMan:
                {
                    DeliverymanNotification deliveryman =
                        nextStep.Deliveryman;

                    sb.AppendLine("*نوع المستلم:* مندوب توصيل");
                    sb.AppendLine($"*الاسم:* {deliveryman.Name}");
                    sb.AppendLine($"*الهاتف:* {deliveryman.Phone}");
                    sb.AppendLine($"*العنوان:* {deliveryman.City}");

                    break;
                }

            default:
                throw new InvalidOperationException(
                    $"Unsupported delivery method: {nextStep.DeliveryMethod}");
        }
    }

    protected static void AppendFooter(StringBuilder sb)
    {
        sb.AppendLine("----------------------");
    }

    protected static string GetDisplayTitle(
        OrderNotification order,
        RecipientType recipientType,
        int entityId)
    {
        return order.EntitySequences
            .FirstOrDefault(x =>
                x.RecipientType == recipientType &&
                x.EntityId == entityId)
            ?.DisplayTitle
            ?? order.OrderNumber;
    }
}