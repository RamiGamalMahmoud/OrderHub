using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services;

public interface IFileStorageService
{
    Task SaveFileAsync(byte[] pdfBytes, string fillePath);

    StoredFile SaveAttachment(string sourceFilePath, string destinationName);
    Task SaveQuotationDocumentAsync(byte[] document, string fileName);
    Task SaveInvoiceDocumentAsync(byte[] document, string fileName);
    Task SaveProformaInvoiceDocumentAsync(byte[] document, string fileName);
    void RemoveAttachmentFile(string path);
}

public sealed record StoredFile(
    string FileName,
    string Extension,
    long Size);