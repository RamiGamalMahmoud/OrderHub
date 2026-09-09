using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services;

public interface IFileStorageService
{
    Task<StoredFile> SaveAttachmentAsync(
        string sourceFilePath,
        string destinationName,
        CancellationToken cancellationToken = default);

    Task<byte[]> GetAttachmentAsync(
        string fileName,
        CancellationToken cancellationToken = default);

    Task DeleteAttachmentAsync(
        string fileName,
        CancellationToken cancellationToken = default);


    Task SaveQuotationDocumentAsync(
        byte[] document,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<byte[]> GetQuotationDocumentAsync(
        string fileName,
        CancellationToken cancellationToken = default);

    Task DeleteQuotationDocumentAsync(
        string fileName,
        CancellationToken cancellationToken = default);


    Task SaveProformaInvoiceDocumentAsync(
        byte[] document,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<byte[]> GetProformaInvoiceDocumentAsync(
        string fileName,
        CancellationToken cancellationToken = default);

    Task DeleteProformaInvoiceDocumentAsync(
        string fileName,
        CancellationToken cancellationToken = default);


    Task SaveInvoiceDocumentAsync(
        byte[] document,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<byte[]> GetInvoiceDocumentAsync(
        string fileName,
        CancellationToken cancellationToken = default);

    Task DeleteInvoiceDocumentAsync(
        string fileName,
        CancellationToken cancellationToken = default);
}

public sealed record StoredFile(
    string FileName,
    string Extension,
    long Size);