using System;

namespace OrderHub.Application.Features.Inventory;

public static class StockAttachmentRules
{
    public const long MaxSizeBytes = 5 * 1024 * 1024;

    private static readonly byte[] PdfSignature = [(byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-'];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];

    public static bool TryResolve(
        byte[] content,
        string originalFileName,
        out string contentType,
        out string extension,
        out string error)
    {
        contentType = null;
        extension = null;
        error = null;

        if (content is null || content.Length == 0)
        {
            error = "الملف فارغ.";
            return false;
        }

        if (content.LongLength > MaxSizeBytes)
        {
            error = "حجم الملف يتجاوز 5 ميجابايت.";
            return false;
        }

        string requestedExtension = System.IO.Path
            .GetExtension(originalFileName ?? string.Empty)
            .ToLowerInvariant();

        switch (requestedExtension)
        {
            case ".pdf":
                if (!StartsWith(content, PdfSignature))
                {
                    error = "محتوى الملف لا يطابق صيغة PDF.";
                    return false;
                }

                contentType = "application/pdf";
                extension = ".pdf";
                return true;

            case ".jpg":
            case ".jpeg":
                if (!StartsWith(content, JpegSignature))
                {
                    error = "محتوى الملف لا يطابق صيغة JPG.";
                    return false;
                }

                contentType = "image/jpeg";
                extension = ".jpg";
                return true;

            case ".png":
                if (!StartsWith(content, PngSignature))
                {
                    error = "محتوى الملف لا يطابق صيغة PNG.";
                    return false;
                }

                contentType = "image/png";
                extension = ".png";
                return true;

            default:
                error = "يُسمح بمرفقات PDF أو JPG أو PNG فقط.";
                return false;
        }
    }

    public static string NormalizeDescription(string description, out string error)
    {
        error = null;

        string trimmed = description?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;

        if (trimmed.Length > OrderHub.Domain.Models.StockReceiptAttachment.DescriptionMaxLength)
        {
            error = "وصف المرفق يتجاوز 200 حرف.";
            return null;
        }

        return trimmed;
    }

    private static bool StartsWith(byte[] content, byte[] signature)
    {
        if (content.Length < signature.Length)
            return false;

        for (int index = 0; index < signature.Length; index++)
        {
            if (content[index] != signature[index])
                return false;
        }

        return true;
    }
}
