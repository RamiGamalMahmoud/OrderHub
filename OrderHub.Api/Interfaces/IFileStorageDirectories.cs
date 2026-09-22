namespace OrderHub.Api.Interfaces;

public interface IFileStorageDirectories
{
    string AppDirectory { get; }

    string StorageDirectory { get; }

    string AttachmentsDirectory { get; }

    string DocumentsDirectory { get; }

    string InvoicesDirectory { get; }

    string ProformaInvoicesDirectory { get; }

    string QuotationsDirectory { get; }

    string LogsDirectory { get; }

    void EnsureDirectoriesCreated();
}