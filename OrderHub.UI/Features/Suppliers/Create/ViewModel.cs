using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Features.Suppliers.Editor;
using OrderHub.UI.Interfaces;
using System;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.SupplierDtos;

namespace OrderHub.UI.Features.Suppliers.Create;

public partial class ViewModel : EditSupplierViewModelBase
{
    private readonly IMessenger _messenger;

    public ViewModel(IMediator mediator, IMessenger messenger, IDialogService dialogService) : base(mediator, dialogService)
    {
        _messenger = messenger;
    }

    protected override async Task Save()
    {
        SupplierCreateDto supplierCreateDto = new SupplierCreateDto(
            Name, 
            TimeOnly.FromDateTime(OpenAt.Value), TimeOnly.FromDateTime(CloseAt.Value), 
            Street, City.Id, 
            Number, CountryCode);

        Result result = await _mediator.Send(new Application.Commands.SupplierCommands.CreateSupplierCommand(supplierCreateDto));

        if(result.IsSuccess)
        {
            await _mediator.Publish(new Application.Notifications.SuccessNotification("تم إضافة المورد بنجاح"));
            _messenger.Send(new Application.Messages.Suppliers.SupplierCreatedMessage());
            OnRequestClose();
        }
        else
        {
            await _mediator.Publish(new Application.Notifications.ErrorNotification("فشل في إضافة المورد"));
        }
    }
}
