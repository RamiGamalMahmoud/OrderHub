using OrderHub.Application.Interfaces.Services;
using System.IO;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services;

internal class LocalFileStorageService : IFileStorageService
{
    private readonly IApplicationDirectoriesService _directoriesService;

    public LocalFileStorageService(IApplicationDirectoriesService directoriesService)
    {
        _directoriesService = directoriesService;
    }

    public StoredFile SaveAttachment(string sourceFilePath, string destinationName)
    {
        if (!File.Exists(sourceFilePath))
            throw new FileNotFoundException(
                "Attachment file was not found.",
                sourceFilePath);

        string extension = Path.GetExtension(sourceFilePath);

        string destinationPath = Path.Combine(
            _directoriesService.AttachmentsDirectory,
            destinationName);

        File.Copy(sourceFilePath, destinationPath);

        long size = new FileInfo(destinationPath).Length;

        return new StoredFile(
            destinationName,
            extension,
            size);
    }

    public void RemoveAttachmentFile(string fileName)
    {
        string path = Path.Combine(_directoriesService.AttachmentsDirectory, fileName);
        if(File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public async Task SaveFileAsync(byte[] pdfBytes, string fileName)
    {
        string fullPath = Path.Combine(_directoriesService.StorageDirectory, fileName);
        await File.WriteAllBytesAsync(fullPath, pdfBytes);
    }

    public async Task SaveQuotationDocumentAsync(byte[] document, string fileName)
    {
        string documentPath = Path.Combine(_directoriesService.QuotationsDirectory, fileName);
        await File.WriteAllBytesAsync(documentPath, document);
    }

    public async Task SaveInvoiceDocumentAsync(byte[] document, string fileName)
    {
        string documentPath = Path.Combine(_directoriesService.InvoicesDirecoty, fileName);
        await File.WriteAllBytesAsync(documentPath, document);
    }

    public async Task SaveProformaInvoiceDocumentAsync(byte[] document, string fileName)
    {
        string documentPath = Path.Combine(_directoriesService.ProformaInvoicesDirectory, fileName);
        await File.WriteAllBytesAsync(documentPath, document);
    }
}
