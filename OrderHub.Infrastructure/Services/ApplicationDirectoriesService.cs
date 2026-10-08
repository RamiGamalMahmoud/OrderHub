using Microsoft.Extensions.Options;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Infrastructure.Options;
using System.IO;

namespace OrderHub.Infrastructure.Services;

internal sealed class ApplicationDirectoriesService
    : IApplicationDirectoriesService
{
    public ApplicationDirectoriesService(IOptions<ApplicationDataOptions> options)
    {
        RootDirectory = Path.GetFullPath(options.Value.RootPath);
    }

    public string RootDirectory { get; }

    public string AttachmentsDirectory =>
        Path.Combine(RootDirectory, "Attachments");

    public string DocumentsDirectory =>
        Path.Combine(RootDirectory, "Documents");

    public string InvoicesDirectory =>
        Path.Combine(DocumentsDirectory, "Invoices");

    public string ProformaInvoicesDirectory =>
        Path.Combine(DocumentsDirectory, "ProformaInvoices");

    public string QuotationsDirectory =>
        Path.Combine(DocumentsDirectory, "Quotations");

    public string BackupsDirectory =>
        Path.Combine(RootDirectory, "Backups");

    public string LogsDirectory =>
        Path.Combine(RootDirectory, "Logs");

    public void EnsureDirectoriesCreated()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(AttachmentsDirectory);
        Directory.CreateDirectory(DocumentsDirectory);
        Directory.CreateDirectory(InvoicesDirectory);
        Directory.CreateDirectory(ProformaInvoicesDirectory);
        Directory.CreateDirectory(QuotationsDirectory);
        Directory.CreateDirectory(BackupsDirectory);
        Directory.CreateDirectory(LogsDirectory);
    }
}