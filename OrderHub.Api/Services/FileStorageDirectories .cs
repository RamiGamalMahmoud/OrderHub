using OrderHub.Api.Interfaces;
using System;
using System.IO;

namespace OrderHub.Api.Services;

internal sealed class FileStorageDirectories : IFileStorageDirectories
{
    private const string APP_NAME = "OrderHub";

    public string AppDirectory =>
#if DEBUG
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            $"{APP_NAME} - Dev");
#else
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            APP_NAME);
#endif

    public string StorageDirectory =>
        Path.Combine(AppDirectory, "Storage");

    public string AttachmentsDirectory =>
        Path.Combine(StorageDirectory, "Attachments");

    public string DocumentsDirectory =>
        Path.Combine(StorageDirectory, "Documents");

    public string InvoicesDirectory =>
        Path.Combine(DocumentsDirectory, "Invoices");

    public string ProformaInvoicesDirectory =>
        Path.Combine(DocumentsDirectory, "ProformaInvoices");

    public string QuotationsDirectory =>
        Path.Combine(DocumentsDirectory, "Quotations");

    public string LogsDirectory =>
        Path.Combine(StorageDirectory, "Logs");

    public void EnsureDirectoriesCreated()
    {
        Directory.CreateDirectory(AppDirectory);
        Directory.CreateDirectory(StorageDirectory);

        Directory.CreateDirectory(AttachmentsDirectory);

        Directory.CreateDirectory(DocumentsDirectory);
        Directory.CreateDirectory(InvoicesDirectory);
        Directory.CreateDirectory(ProformaInvoicesDirectory);
        Directory.CreateDirectory(QuotationsDirectory);

        Directory.CreateDirectory(LogsDirectory);
    }
}