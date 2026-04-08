using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Helpers;

internal static class OrderEntitySequenceManager
{
    public static async Task SyncAsync(AppDbContext appDbContext, Order order, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(appDbContext);
        ArgumentNullException.ThrowIfNull(order);

        List<(RecipientType RecipientType, int EntityId)> requiredParticipants = GetRequiredParticipants(order);
        HashSet<(RecipientType RecipientType, int EntityId)> requiredKeys = requiredParticipants.ToHashSet();

        foreach (OrderEntitySequence obsoleteSequence in order.EntitySequences
                     .Where(sequence => !requiredKeys.Contains((sequence.RecipientType, sequence.EntityId)))
                     .ToList())
        {
            order.RemoveEntitySequence(obsoleteSequence);
            appDbContext.OrderEntitySequences.Remove(obsoleteSequence);
        }

        HashSet<(RecipientType RecipientType, int EntityId)> existingKeys = order.EntitySequences
            .Select(sequence => (sequence.RecipientType, sequence.EntityId))
            .ToHashSet();

        foreach ((RecipientType recipientType, int entityId) in requiredParticipants)
        {
            if (existingKeys.Contains((recipientType, entityId)))
            {
                continue;
            }

            int nextSequenceNumber = await GetNextSequenceNumberAsync(
                appDbContext,
                recipientType,
                entityId,
                order.CreatedAt,
                cancellationToken);

            order.AddEntitySequence(new OrderEntitySequence(
                recipientType,
                entityId,
                order.CreatedAt.Year,
                order.CreatedAt.Month,
                nextSequenceNumber,
                OrderEntitySequenceFormatter.Format(recipientType, nextSequenceNumber, order.CreatedAt)));
        }
    }

    private static async Task<int> GetNextSequenceNumberAsync(
        AppDbContext appDbContext,
        RecipientType recipientType,
        int entityId,
        DateTime createdAt,
        CancellationToken cancellationToken)
    {
        int lastSequenceNumber = await appDbContext.OrderEntitySequences
            .Where(sequence =>
                sequence.RecipientType == recipientType
                && sequence.EntityId == entityId
                && sequence.SequenceYear == createdAt.Year
                && sequence.SequenceMonth == createdAt.Month)
            .Select(sequence => (int?)sequence.SequenceNumber)
            .MaxAsync(cancellationToken)
            ?? 0;

        return lastSequenceNumber + 1;
    }

    private static List<(RecipientType RecipientType, int EntityId)> GetRequiredParticipants(Order order)
    {
        IEnumerable<(RecipientType RecipientType, int EntityId)> suppliers = order.OrderItems
            .Where(item => item.SupplierId.HasValue)
            .Select(item => (RecipientType.Supplier, item.SupplierId!.Value));

        IEnumerable<(RecipientType RecipientType, int EntityId)> deliverymen = GetDeliverymanIds(order)
            .Select(deliverymanId => (RecipientType.Deliveryman, deliverymanId));

        return new[] { (RecipientType.Client, order.ClientId) }
            .Concat(suppliers)
            .Concat(deliverymen)
            .Distinct()
            .ToList();
    }

    private static IEnumerable<int> GetDeliverymanIds(Order order)
    {
        if (order.DeliverymanId.HasValue)
        {
            yield return order.DeliverymanId.Value;
        }

        foreach (int deliverymanId in order.DeliverySteps
                     .Where(step => step.DeliverymanId.HasValue)
                     .Select(step => step.DeliverymanId!.Value)
                     .Distinct())
        {
            yield return deliverymanId;
        }
    }
}
