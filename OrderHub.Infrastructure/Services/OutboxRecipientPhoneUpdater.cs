using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services;

internal static class OutboxRecipientPhoneUpdater
{
    public static async Task UpdateClientPhoneAsync(AppDbContext dbContext, int clientId, string phoneNumber, CancellationToken cancellationToken)
    {
        var recipients = await dbContext.ClientRecipients
            .Join(
                dbContext.OutboxMessages.Where(message => message.Status != OutboxMessageStatus.Sent),
                recipient => recipient.Id,
                message => message.RecipientId,
                (recipient, _) => recipient)
            .Where(recipient => recipient.ClientId == clientId)
            .ToListAsync(cancellationToken);

        foreach (ClientRecipient recipient in recipients)
        {
            recipient.PhoneNumber = phoneNumber;
        }
    }

    public static async Task UpdateSupplierPhoneAsync(AppDbContext dbContext, int supplierId, string phoneNumber, CancellationToken cancellationToken)
    {
        var recipients = await dbContext.SupplierRecipients
            .Join(
                dbContext.OutboxMessages.Where(message => message.Status != OutboxMessageStatus.Sent),
                recipient => recipient.Id,
                message => message.RecipientId,
                (recipient, _) => recipient)
            .Where(recipient => recipient.SupplierId == supplierId)
            .ToListAsync(cancellationToken);

        foreach (SupplierRecipient recipient in recipients)
        {
            recipient.PhoneNumber = phoneNumber;
        }
    }

    public static async Task UpdateShippingCarrierPhoneAsync(AppDbContext dbContext, int shippingCarrierId, string phoneNumber, CancellationToken cancellationToken)
    {
        var recipients = await dbContext.ShippingCarrierRecipients
            .Join(
                dbContext.OutboxMessages.Where(message => message.Status != OutboxMessageStatus.Sent),
                recipient => recipient.Id,
                message => message.RecipientId,
                (recipient, _) => recipient)
            .Where(recipient => recipient.ShippingCarrierId == shippingCarrierId)
            .ToListAsync(cancellationToken);

        foreach (ShippingCarrierRecipient recipient in recipients)
        {
            recipient.PhoneNumber = phoneNumber;
        }
    }

    public static async Task UpdateDeliverymanPhoneAsync(AppDbContext dbContext, int deliverymanId, string phoneNumber, CancellationToken cancellationToken)
    {
        var recipients = await dbContext.DeliverymanRecipients
            .Join(
                dbContext.OutboxMessages.Where(message => message.Status != OutboxMessageStatus.Sent),
                recipient => recipient.Id,
                message => message.RecipientId,
                (recipient, _) => recipient)
            .Where(recipient => recipient.DeliveryManId == deliverymanId)
            .ToListAsync(cancellationToken);

        foreach (DeliverymanRecipient recipient in recipients)
        {
            recipient.PhoneNumber = phoneNumber;
        }
    }
}
