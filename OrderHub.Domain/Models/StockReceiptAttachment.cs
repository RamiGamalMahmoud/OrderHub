using System;

namespace OrderHub.Domain.Models;

public sealed class StockReceiptAttachment : ModelBase
{
    public const int DescriptionMaxLength = 200;
    public const int OriginalFileNameMaxLength = 255;
    public const int ContentTypeMaxLength = 100;
    public const int StoredFileNameMaxLength = 260;

    private StockReceiptAttachment() { }

    private StockReceiptAttachment(
        int warehouseId,
        string storedFileName,
        string originalFileName,
        string description,
        long sizeBytes,
        string contentType,
        int uploadedByUserId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(warehouseId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(uploadedByUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(storedFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        if (storedFileName.Length > StoredFileNameMaxLength)
            throw new ArgumentOutOfRangeException(nameof(storedFileName), "اسم الملف المخزن طويل جدًا.");

        if (originalFileName.Length > OriginalFileNameMaxLength)
            throw new ArgumentOutOfRangeException(nameof(originalFileName), "اسم الملف الأصلي طويل جدًا.");

        if (contentType.Length > ContentTypeMaxLength)
            throw new ArgumentOutOfRangeException(nameof(contentType), "نوع الملف غير صالح.");

        if (sizeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "حجم الملف يجب أن يكون أكبر من صفر.");

        string trimmedDescription = description?.Trim();
        if (string.IsNullOrEmpty(trimmedDescription))
            trimmedDescription = null;

        if (trimmedDescription?.Length > DescriptionMaxLength)
            throw new ArgumentOutOfRangeException(nameof(description), "وصف المرفق يتجاوز 200 حرف.");

        WarehouseId = warehouseId;
        StoredFileName = storedFileName;
        OriginalFileName = originalFileName;
        Description = trimmedDescription;
        SizeBytes = sizeBytes;
        ContentType = contentType;
        UploadedByUserId = uploadedByUserId;
    }

    public static StockReceiptAttachment CreateDraft(
        int warehouseId,
        string storedFileName,
        string originalFileName,
        string description,
        long sizeBytes,
        string contentType,
        int uploadedByUserId) =>
        new(warehouseId, storedFileName, originalFileName, description, sizeBytes, contentType, uploadedByUserId);

    public int? StockReceiptId { get; private set; }

    public StockReceipt StockReceipt { get; private set; }

    public int WarehouseId { get; private set; }

    public Warehouse Warehouse { get; private set; }

    public string StoredFileName { get; private set; }

    public string OriginalFileName { get; private set; }

    public string Description { get; private set; }

    public long SizeBytes { get; private set; }

    public string ContentType { get; private set; }

    public int UploadedByUserId { get; private set; }

    public User UploadedBy { get; private set; }

    public bool IsDraft => StockReceiptId is null;

    public void AttachTo(int stockReceiptId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stockReceiptId);

        if (StockReceiptId is not null)
            throw new InvalidOperationException("المرفق مرتبط بتوريد محفوظ بالفعل.");

        StockReceiptId = stockReceiptId;
    }
}
