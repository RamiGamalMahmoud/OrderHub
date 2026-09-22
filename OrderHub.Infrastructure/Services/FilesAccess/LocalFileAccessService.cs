//using OrderHub.Application.Interfaces.Services;
//using System;
//using System.IO;
//using System.Threading.Tasks;

//namespace OrderHub.Infrastructure.Services.FilesAccess;

//internal sealed class LocalFileAccessService
//    : ILocalFileAccessService
//{
//    private readonly IApplicationDirectoriesService _directories;

//    public LocalFileAccessService(
//        IApplicationDirectoriesService directories)
//    {
//        _directories = directories;
//    }

//    // Attachments

//    public async Task SaveAttachmentAsync(
//        string fileName,
//        Stream content)
//    {
//        string filePath = GetAttachmentPath(fileName);

//        await SaveAsync(filePath, content);
//    }

//    public Task<string> GetAttachmentAsync(
//        string fileName)
//    {
//        string filePath = GetAttachmentPath(fileName);

//        return Task.FromResult(
//            File.Exists(filePath)
//                ? filePath
//                : null);
//    }

//    public Task DeleteAttachmentAsync(
//        string fileName)
//    {
//        DeleteFile(GetAttachmentPath(fileName));

//        return Task.CompletedTask;
//    }

//    // Quotations

//    public async Task SaveQuotationAsync(
//        string fileName,
//        byte[] content)
//    {
//        string filePath = GetQuotationPath(fileName);

//        await SaveAsync(filePath, content);
//    }

//    public Task<string> GetQuotationAsync(
//        string fileName)
//    {
//        string filePath = GetQuotationPath(fileName);

//        return Task.FromResult(
//            File.Exists(filePath)
//                ? filePath
//                : null);
//    }

//    public Task DeleteQuotationAsync(
//        string fileName)
//    {
//        DeleteFile(GetQuotationPath(fileName));

//        return Task.CompletedTask;
//    }

//    // Invoices

//    public async Task SaveInvoiceAsync(
//        string fileName,
//        byte[] content)
//    {
//        string filePath = GetInvoicePath(fileName);

//        await SaveAsync(filePath, content);
//    }

//    public Task<string> GetInvoiceAsync(
//        string fileName)
//    {
//        string filePath = GetInvoicePath(fileName);

//        return Task.FromResult(
//            File.Exists(filePath)
//                ? filePath
//                : null);
//    }

//    public Task DeleteInvoiceAsync(
//        string fileName)
//    {
//        DeleteFile(GetInvoicePath(fileName));

//        return Task.CompletedTask;
//    }

//    // Proforma Invoices

//    public async Task SaveProformaInvoiceAsync(
//        string fileName,
//        byte[] content)
//    {
//        string filePath =
//            GetProformaInvoicePath(fileName);

//        await SaveAsync(filePath, content);
//    }

//    public Task<string> GetProformaInvoiceAsync(
//        string fileName)
//    {
//        string filePath =
//            GetProformaInvoicePath(fileName);

//        return Task.FromResult(
//            File.Exists(filePath)
//                ? filePath
//                : null);
//    }

//    public Task DeleteProformaInvoiceAsync(
//        string fileName)
//    {
//        DeleteFile(
//            GetProformaInvoicePath(fileName));

//        return Task.CompletedTask;
//    }

//    // Paths

//    private string GetAttachmentPath(
//        string fileName)
//    {
//        ValidateFileName(fileName);

//        return Path.Combine(
//            _directories.AttachmentsDirectory,
//            fileName);
//    }

//    private string GetQuotationPath(
//        string fileName)
//    {
//        ValidateFileName(fileName);

//        return Path.Combine(
//            _directories.QuotationsDirectory,
//            fileName);
//    }

//    private string GetInvoicePath(
//        string fileName)
//    {
//        ValidateFileName(fileName);

//        return Path.Combine(
//            _directories.InvoicesDirecoty,
//            fileName);
//    }

//    private string GetProformaInvoicePath(
//        string fileName)
//    {
//        ValidateFileName(fileName);

//        return Path.Combine(
//            _directories.ProformaInvoicesDirectory,
//            fileName);
//    }

//    private static async Task SaveAsync(
//        string filePath,
//        Stream content)
//    {
//        string directory =
//            Path.GetDirectoryName(filePath);

//        if (!string.IsNullOrEmpty(directory))
//            Directory.CreateDirectory(directory);

//        await using FileStream fileStream = new(
//            filePath,
//            FileMode.Create,
//            FileAccess.Write,
//            FileShare.None,
//            bufferSize: 81920,
//            useAsync: true);

//        await content.CopyToAsync(fileStream);
//    }

//    private static async Task SaveAsync(
//        string filePath,
//        byte[] content)
//    {
//        string directory =
//            Path.GetDirectoryName(filePath);

//        if (!string.IsNullOrEmpty(directory))
//            Directory.CreateDirectory(directory);

//        await File.WriteAllBytesAsync(
//            filePath,
//            content);
//    }

//    private static void DeleteFile(
//        string filePath)
//    {
//        if (File.Exists(filePath))
//            File.Delete(filePath);
//    }

//    private static void ValidateFileName(
//        string fileName)
//    {
//        if (string.IsNullOrWhiteSpace(fileName) ||
//            fileName != Path.GetFileName(fileName))
//        {
//            throw new ArgumentException(
//                "Invalid file name.",
//                nameof(fileName));
//        }
//    }

//    public bool FileExists(string fileName)
//    {
//        return Path.Exists(fileName);
//    }
//}