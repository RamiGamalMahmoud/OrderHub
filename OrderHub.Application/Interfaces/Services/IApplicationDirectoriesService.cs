namespace OrderHub.Application.Interfaces.Services;

public interface IApplicationDirectoriesService
{
    string AppDirectory { get; }
    string StorageDirectory { get; }
    string CredentialsFilePath { get; }
    string TokenFilePath { get; }
    string DatabaseFilePath { get; }
    string WhatsAppProfilesDirectory { get; }
    string DefaultWhatsAppProfileDirectory { get; }
    string LogsDirectory { get; }
    string DataDirectory { get; }
    string DraftsDirectory { get; }
    //string DocumentsDirectory { get; }
    string AttachmentsDirectory { get; }
    string InvoicesDirecoty { get; }
    string ProformaInvoicesDirectory { get; }
    string QuotationsDirectory { get; }

    void EnsureDirectoriesCreated();
}