using CommunityToolkit.Mvvm.Input;
using OrderHub.Application.Features.Documents.Invoices;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Enums;
using OrderHub.UI.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace OrderHub.UI.Commands;

public partial class DocumentsCommands
    : AuthorizableCommandGroup, ICommandGroup
{
    private readonly IRequestExecutor _requestExecutor;
    private readonly IFileStorageService _fileStorageService;
    private readonly IServerConfiguration _serverConfiguration;

    public DocumentsCommands(
        IAuthorizationService authorization,
        IRequestExecutor requestExecutor,
        IFileStorageService fileStorageService,
        IServerConfiguration serverConfiguration)
        : base(authorization)
    {
        _requestExecutor = requestExecutor;

        RegisterAuthorizableCommands(
            GenerateInvoiceCommand,
            GenerateDeliveryNoteCommand);
        _fileStorageService = fileStorageService;
        _serverConfiguration = serverConfiguration;
    }

    private bool CanWrite =>
        Authorization.CanWrite(PermissionFeature.Documents);

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task GenerateInvoice(int orderId)
    {
        string invoiceNumber =
            await _requestExecutor.ExecuteAsync(
                new CreateInvoiceCommand.Command(orderId));

        await DialogService.Instance
            .ShowDialog<Features.Documents.Preview.PreviewDocumentView>(
                "عرض فاتورة",
                new Features.Documents.Preview.Data(
                    $"{invoiceNumber}.pdf",
                    DocumentType.Invoice,
                    "args.ClientName",
                    "args.ClientPhoneNumber"));
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private Task GenerateDeliveryNote(int orderId)
    {
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SaveFileFromUrlAsync(Uri uri)
    {
        string url = uri.ToString();
        string fileName = GetFileNameFromUrl(url);
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
        await _fileStorageService.SaveFileAsync(
            url,
            path);

        ShowInFolder(path);
    }

    private void ShowInFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (!File.Exists(path))
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{path}\"",
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private void OpenInBrowser(Uri uri)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = uri.AbsoluteUri,
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private void CreateQrCode(Uri uri)
    {
        var qrCode = QrCodeGenerator.Generate(uri.AbsoluteUri);
        DialogService.Instance.ShowDialog(new Services.Views.QrCodeViewer(qrCode), "رابط الملف");
    }

    private static string GetFileNameFromUrl(string url)
    {
        return Path.GetFileName(new Uri(url).LocalPath);
    }
}

public sealed record GenerateInvoiceArgs(
    int OrderId,
    string ClientName,
    string ClientPhoneNumber);
