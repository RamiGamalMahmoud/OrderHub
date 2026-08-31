using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Features.Suppliers.Editor;
using OrderHub.UI.Services;
using System;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.SupplierDtos;

namespace OrderHub.UI.Features.Suppliers.Create;

public partial class ViewModel : EditSupplierViewModelBase
{
    public ViewModel(IMediator mediator) : base(mediator)
    {
    }

    protected override async Task Save()
    {
        SupplierCreateDto supplierCreateDto = new SupplierCreateDto(
            Name, 
            TimeOnly.FromDateTime(OpenAt.Value), TimeOnly.FromDateTime(CloseAt.Value), 
            Street, City.Id, 
            Number, CountryCode, SelectedWhatsappGroup.Id);

        Result result = await _mediator.Send(new Application.Commands.SupplierCommands.CreateSupplierCommand(supplierCreateDto));

        if(result.IsSuccess)
        {
            NotificationService.Instance.ShowSuccess("تم إضافة المورد بنجاح");
            WeakReferenceMessenger.Default.Send(new Application.Messages.Suppliers.SupplierCreatedMessage());
            OnRequestClose();
        }
        else
        {
            NotificationService.Instance.ShowError("فشل في إضافة المورد");
        }
    }
}
