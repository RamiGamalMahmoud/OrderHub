//using OrderHub.Application.Interfaces.Services;
//using System.IO;
//using System.Threading;
//using System.Threading.Tasks;

//namespace OrderHub.Infrastructure.Services.FilesAccess;

//internal sealed class LocalFileStorageService
//    : IFileStorageService
//{
//    private readonly ILocalFileAccessService _localFileAccessService;

//    public LocalFileStorageService(
//        ILocalFileAccessService localFileAccessService)
//    {
//        _localFileAccessService = localFileAccessService;
//    }


//    #region Attachments

//    public async Task<StoredFile> SaveAttachmentAsync(
//        string sourceFilePath,
//        string destinationName,
//        CancellationToken cancellationToken = default)
//    {
//        await using FileStream fileStream = new(
//            sourceFilePath,
//            FileMode.Open,
//            FileAccess.Read,
//            FileShare.Read);

//        await _localFileAccessService.SaveAttachmentAsync(
//            destinationName,
//            fileStream);

//        FileInfo fileInfo = new(sourceFilePath);

//        return new StoredFile(
//            destinationName,
//            fileInfo.Extension,
//            fileInfo.Length);
//    }

//    public async Task<byte[]> GetAttachmentAsync(
//        string fileName,
//        CancellationToken cancellationToken = default)
//    {
//        string filePath =
//            await _localFileAccessService.GetAttachmentAsync(
//                fileName);

//        return await File.ReadAllBytesAsync(
//            filePath,
//            cancellationToken);
//    }

//    public Task DeleteAttachmentAsync(
//        string fileName,
//        CancellationToken cancellationToken = default)
//    {
//        return _localFileAccessService.DeleteAttachmentAsync(
//            fileName);
//    }

//    #endregion


//    #region Quotations

//    public async Task SaveQuotationDocumentAsync(
//        byte[] document,
//        string fileName,
//        CancellationToken cancellationToken = default)
//    {
//        await _localFileAccessService.SaveQuotationAsync(
//            fileName,
//            document);
//    }

//    public async Task<byte[]> GetQuotationDocumentAsync(
//        string fileName,
//        CancellationToken cancellationToken = default)
//    {
//        string filePath =
//            await _localFileAccessService.GetQuotationAsync(
//                fileName);

//        return await File.ReadAllBytesAsync(
//            filePath,
//            cancellationToken);
//    }

//    public Task DeleteQuotationDocumentAsync(
//        string fileName,
//        CancellationToken cancellationToken = default)
//    {
//        return _localFileAccessService.DeleteQuotationAsync(
//            fileName);
//    }

//    #endregion


//    #region Invoices

//    public async Task SaveInvoiceDocumentAsync(
//        byte[] document,
//        string fileName,
//        CancellationToken cancellationToken = default)
//    {
//        await _localFileAccessService.SaveInvoiceAsync(
//            fileName,
//            document);
//    }

//    public async Task<byte[]> GetInvoiceDocumentAsync(
//        string fileName,
//        CancellationToken cancellationToken = default)
//    {
//        string filePath =
//            await _localFileAccessService.GetInvoiceAsync(
//                fileName);

//        return await File.ReadAllBytesAsync(
//            filePath,
//            cancellationToken);
//    }

//    public Task DeleteInvoiceDocumentAsync(
//        string fileName,
//        CancellationToken cancellationToken = default)
//    {
//        return _localFileAccessService.DeleteInvoiceAsync(
//            fileName);
//    }

//    #endregion


//    #region Proforma Invoices

//    public async Task SaveProformaInvoiceDocumentAsync(
//        byte[] document,
//        string fileName,
//        CancellationToken cancellationToken = default)
//    {
//        await _localFileAccessService.SaveProformaInvoiceAsync(
//            fileName,
//            document);
//    }

//    public async Task<byte[]> GetProformaInvoiceDocumentAsync(
//        string fileName,
//        CancellationToken cancellationToken = default)
//    {
//        string filePath =
//            await _localFileAccessService.GetProformaInvoiceAsync(
//                fileName);

//        return await File.ReadAllBytesAsync(
//            filePath,
//            cancellationToken);
//    }

//    public Task DeleteProformaInvoiceDocumentAsync(
//        string fileName,
//        CancellationToken cancellationToken = default)
//    {
//        return _localFileAccessService.DeleteProformaInvoiceAsync(
//            fileName);
//    }

//    #endregion
//}