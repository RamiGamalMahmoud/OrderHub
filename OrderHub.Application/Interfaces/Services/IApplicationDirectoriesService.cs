namespace OrderHub.Application.Interfaces.Services;


public interface IApplicationDirectoriesService
{
    string RootDirectory { get; }

    string ConfigDirectory { get; }

    string LogsDirectory { get; }

    string AttachmentsDirectory { get; }

    string DocumentsDirectory { get; }

    string InvoicesDirectory { get; }

    string ProformaInvoicesDirectory { get; }

    string QuotationsDirectory { get; }

    string BackupsDirectory { get; }

    string DataProtectionDirectory { get; }

    void EnsureDirectoriesCreated();
}
