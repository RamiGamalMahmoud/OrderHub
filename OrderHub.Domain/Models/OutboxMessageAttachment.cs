using System;

namespace OrderHub.Domain.Models;

public class OutboxMessageAttachment : ModelBase
{
    public string OriginalFileName { get; private set; }
    public string StoredFileName { get; private set; }
    public string RelativePath { get; private set; }
    public string ContentType { get; private set; }
    public long FileSize { get; private set; }

    public int OutboxMessageId { get; private set; }
    public OutboxMessage OutboxMessage { get; private set; }
    private OutboxMessageAttachment() { }

    public OutboxMessageAttachment(string originalFileName, string storedFileName, string extension, long fileSize, int outboxMessageId) :
        this(originalFileName, storedFileName, extension, fileSize)
    {

        OutboxMessageId = outboxMessageId;
    }

    public OutboxMessageAttachment(string originalFileName, string storedFileName, string extension, long fileSize)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ArgumentException("Original file name cannot be empty.", nameof(originalFileName));

        if (string.IsNullOrWhiteSpace(extension))
            throw new ArgumentException("Extension cannot be empty.", nameof(extension));

        if (fileSize <= 0)
            throw new ArgumentException("File size must be greater than zero.", nameof(fileSize));


        OriginalFileName = originalFileName;
        FileSize = fileSize;
        CreatedAt = DateTime.UtcNow;

        StoredFileName = storedFileName;
        ContentType = MapExtensionToMimeType(extension);
    }

    public bool IsValidForWhatsApp()
    {
        const long maxWhatsAppSize = 100 * 1024 * 1024;
        return FileSize <= maxWhatsAppSize;
    }

    private static string MapExtensionToMimeType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".txt" => "text/plain",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }
}
