using OrderHub.Application.Interfaces.Services;
using System;
using System.IO;

namespace OrderHub.Infrastructure.Services;

internal class ApplicationDirectoriesService : IApplicationDirectoriesService
{
    private const string _appName = "OrderHub";

    public string AppDirectory =>
#if DEBUG
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            $"{_appName} - Dev");
#else
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppName);
#endif

    public string StorageDirectory => Path.Combine(AppDirectory, "Storage");

    public string AttachmentsDirectory => Path.Combine(StorageDirectory, "Attachments");

    public string DataDirectory => Path.Combine(AppDirectory, "Data");

    public string WhatsAppProfilesDirectory => Path.Combine(AppDirectory, "WhatsAppProfiles");

    public string DefaultWhatsAppProfileDirectory => Path.Combine(WhatsAppProfilesDirectory, "Default");

    public string DocumentsDirectory => Path.Combine(AppDirectory, "Documents");

    public string DraftsDirectory => Path.Combine(AppDirectory, "Drafts");

    public string LogsDirectory => Path.Combine(StorageDirectory, "Logs");

    public string CredentialsFilePath => Path.Combine(StorageDirectory, "creditals.bin");

    public string TokenFilePath => Path.Combine(StorageDirectory, "token.bin");

    public string DatabaseFilePath => Path.Combine(DataDirectory, "order_hub.db");

    public string InvoicesDirecoty => Path.Combine(DocumentsDirectory, "Invoices");
    public string ProformaInvoicesDirectory => Path.Combine(DocumentsDirectory, "ProformaInvoices");
    public string QuotationsDirectory => Path.Combine(DocumentsDirectory, "Quotations");

    public void EnsureDirectoriesCreated()
    {
        Directory.CreateDirectory(AppDirectory);
        Directory.CreateDirectory(StorageDirectory);
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(WhatsAppProfilesDirectory);
        Directory.CreateDirectory(DefaultWhatsAppProfileDirectory);
        Directory.CreateDirectory(DocumentsDirectory);
        Directory.CreateDirectory(DraftsDirectory);
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(AttachmentsDirectory);
        Directory.CreateDirectory(InvoicesDirecoty);
        Directory.CreateDirectory(ProformaInvoicesDirectory);
        Directory.CreateDirectory(QuotationsDirectory);

        if (!File.Exists(DatabaseFilePath))
        {
            var sourcePath = Path.Combine(
                AppContext.BaseDirectory,
                "Data",
                "order_hub.db");

            File.Copy(sourcePath, DatabaseFilePath);
        }
    }
}