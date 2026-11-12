using System;
using System.Collections.Generic;
using System.Linq;
using OrderHub.Domain.Enums;

namespace OrderHub.Domain.Models;

public sealed class StockReceipt : ModelBase
{
    public const int ReferenceNoteMaxLength = 300;

    private readonly List<StockReceiptLine> _lines = [];
    private readonly List<StockReceiptAttachment> _attachments = [];

    private StockReceipt() { }

    private StockReceipt(
        int warehouseId,
        StockReceiptType receiptType,
        int? supplierId,
        string referenceNote,
        int createdByUserId,
        DateTime createdAtUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(warehouseId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(createdByUserId);

        if (!Enum.IsDefined(receiptType))
            throw new ArgumentOutOfRangeException(nameof(receiptType), "نوع التوريد غير صالح.");

        if (supplierId is <= 0)
            throw new ArgumentOutOfRangeException(nameof(supplierId), "المورد غير صالح.");

        string trimmedNote = referenceNote?.Trim();
        if (string.IsNullOrEmpty(trimmedNote))
            trimmedNote = null;

        if (trimmedNote?.Length > ReferenceNoteMaxLength)
            throw new ArgumentOutOfRangeException(nameof(referenceNote), "الملاحظة المرجعية تتجاوز 300 حرف.");

        WarehouseId = warehouseId;
        ReceiptType = receiptType;
        SupplierId = supplierId;
        ReferenceNote = trimmedNote;
        CreatedByUserId = createdByUserId;

        if (createdAtUtc != default)
            UpdateCreationDate(createdAtUtc);
    }

    public static StockReceipt Create(
        int warehouseId,
        StockReceiptType receiptType,
        int? supplierId,
        string referenceNote,
        int createdByUserId,
        IEnumerable<StockReceiptLineData> lines,
        DateTime createdAtUtc = default)
    {
        ArgumentNullException.ThrowIfNull(lines);

        StockReceiptLineData[] requested = lines.ToArray();
        if (requested.Length == 0)
            throw new ArgumentException("يجب إضافة سطر واحد على الأقل للتوريد.", nameof(lines));

        var receipt = new StockReceipt(
            warehouseId,
            receiptType,
            supplierId,
            referenceNote,
            createdByUserId,
            createdAtUtc);

        foreach (var group in requested.GroupBy(line => line.ProductId))
        {
            int quantity = 0;
            foreach (var line in group)
            {
                if (line.ProductId <= 0)
                    throw new ArgumentOutOfRangeException(nameof(lines), "المنتج غير صالح.");

                if (line.Quantity <= 0)
                    throw new ArgumentOutOfRangeException(nameof(lines), "كمية السطر يجب أن تكون أكبر من صفر.");

                quantity = checked(quantity + line.Quantity);
            }

            receipt._lines.Add(new StockReceiptLine(group.Key, quantity));
        }

        return receipt;
    }

    public int WarehouseId { get; private set; }

    public Warehouse Warehouse { get; private set; }

    public StockReceiptType ReceiptType { get; private set; }

    public int? SupplierId { get; private set; }

    public Supplier Supplier { get; private set; }

    public string ReferenceNote { get; private set; }

    public int CreatedByUserId { get; private set; }

    public User CreatedBy { get; private set; }

    public IReadOnlyCollection<StockReceiptLine> Lines => _lines.AsReadOnly();

    public IReadOnlyCollection<StockReceiptAttachment> Attachments => _attachments.AsReadOnly();

    public void Attach(StockReceiptAttachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);

        if (attachment.WarehouseId != WarehouseId)
            throw new InvalidOperationException("المرفق يخص مخزنًا آخر.");

        attachment.AttachTo(Id);
        _attachments.Add(attachment);
    }
}

public readonly record struct StockReceiptLineData(int ProductId, int Quantity);
