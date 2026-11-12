using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Reopsitories;

internal sealed class StockReceiptRepository(AppDbContext context) : IStockReceiptRepository
{
    public void Add(StockReceipt receipt) => context.StockReceipts.Add(receipt);

    public void AddAttachment(StockReceiptAttachment attachment) =>
        context.StockReceiptAttachments.Add(attachment);

    public Task<StockReceiptAttachment> GetAttachmentAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        context.StockReceiptAttachments
            .FirstOrDefaultAsync(attachment => attachment.Id == id, cancellationToken);

    public async Task<IReadOnlyList<StockReceiptAttachment>> GetDraftAttachmentsAsync(
        IReadOnlyCollection<int> ids,
        int warehouseId,
        int uploadedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return [];

        return await context.StockReceiptAttachments
            .Where(attachment => ids.Contains(attachment.Id)
                && attachment.WarehouseId == warehouseId
                && attachment.UploadedByUserId == uploadedByUserId
                && attachment.StockReceiptId == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockReceiptAttachment>> GetStaleDraftsAsync(
        int warehouseId,
        int uploadedByUserId,
        DateTime olderThanUtc,
        CancellationToken cancellationToken = default) =>
        await context.StockReceiptAttachments
            .Where(attachment => attachment.WarehouseId == warehouseId
                && attachment.UploadedByUserId == uploadedByUserId
                && attachment.StockReceiptId == null
                && attachment.CreatedAt < olderThanUtc)
            .ToListAsync(cancellationToken);

    public void RemoveAttachment(StockReceiptAttachment attachment) =>
        context.StockReceiptAttachments.Remove(attachment);
}
