using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Repositories;

public interface IStockReceiptRepository
{
    void Add(StockReceipt receipt);

    void AddAttachment(StockReceiptAttachment attachment);

    /// <summary>Tracked load; returns null when the attachment does not exist.</summary>
    Task<StockReceiptAttachment> GetAttachmentAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>Draft attachments of one keeper/warehouse that match every requested id.</summary>
    Task<IReadOnlyList<StockReceiptAttachment>> GetDraftAttachmentsAsync(
        IReadOnlyCollection<int> ids,
        int warehouseId,
        int uploadedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Drafts older than the cutoff, used to sweep abandoned uploads.</summary>
    Task<IReadOnlyList<StockReceiptAttachment>> GetStaleDraftsAsync(
        int warehouseId,
        int uploadedByUserId,
        DateTime olderThanUtc,
        CancellationToken cancellationToken = default);

    void RemoveAttachment(StockReceiptAttachment attachment);
}
