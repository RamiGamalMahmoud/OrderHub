using CommunityToolkit.Mvvm.Input;
using OrderHub.Application.Features.Documents.Invoices;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Enums;
using OrderHub.UI.Services;
using System.IO;
using System.Threading.Tasks;

namespace OrderHub.UI.Commands;

public partial class DocumentsCommands
    : AuthorizableCommandGroup, ICommandGroup
{
    private readonly IRequestExecutor _requestExecutor;
    private readonly IApplicationDirectoriesService _applicationDirectoriesService;

    public DocumentsCommands(
        IAuthorizationService authorization,
        IRequestExecutor requestExecutor,
        IApplicationDirectoriesService applicationDirectoriesService)
        : base(authorization)
    {
        _requestExecutor = requestExecutor;
        _applicationDirectoriesService = applicationDirectoriesService;

        RegisterAuthorizableCommands(
            GenerateInvoiceCommand,
            GenerateDeliveryNoteCommand);
    }

    private bool CanWrite =>
        Authorization.CanWrite(PermissionFeature.Documents);

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task GenerateInvoice(int orderId)
    {
        string invoiceNumber =
            await _requestExecutor.ExecuteAsync(
                new CreateInvoiceCommand.Command(orderId));

        string invoicePath = Path.Combine(
            _applicationDirectoriesService.InvoicesDirecoty,
            $"{invoiceNumber}.pdf");

        await DialogService.Instance
            .ShowDialog<Features.Documents.Preview.PreviewDocumentView>(
                "عرض فاتورة",
                new Features.Documents.Preview.Data(
                    invoicePath,
                    "args.ClientName",
                    "args.ClientPhoneNumber"));
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private Task GenerateDeliveryNote(int orderId)
    {
        return Task.CompletedTask;
    }
}

public sealed record GenerateInvoiceArgs(
    int OrderId,
    string ClientName,
    string ClientPhoneNumber);
