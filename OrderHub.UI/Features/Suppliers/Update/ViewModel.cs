using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Features.Suppliers.Editor;
using OrderHub.UI.Interfaces;
using OrderHub.UI.Stores.Markers;
using System;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.SupplierDtos;

namespace OrderHub.UI.Features.Suppliers.Update;

public partial class ViewModel : EditSupplierViewModelBase
{
    private readonly ISelectionStore<ISupplierMarker, int> _selectionStore;
    private readonly IMessenger _messenger;

    public ViewModel(IMediator mediator, ISelectionStore<ISupplierMarker, int> selectionStore, IMessenger messenger, IDialogService dialogService) : base(mediator, dialogService)
    {
        _selectionStore = selectionStore;
        _messenger = messenger;
    }

    public override async Task LoadAsync()
    {
        SupplierEditDto SupplierEditDto = await _mediator.Send(new Application.Queries.SupplierQueries.GetSupplierForEditQuery(_selectionStore.Id));
        await base.LoadAsync();

        Name = SupplierEditDto.Name;
        OpenAt = DateTime.Today.Add(SupplierEditDto.OpenAt.ToTimeSpan());
        CloseAt = DateTime.Today.Add(SupplierEditDto.CloseAt.ToTimeSpan());
        Street = SupplierEditDto.Street;
        City = Cities.Where(c => c.Id == SupplierEditDto.CityId).FirstOrDefault();
        Number = SupplierEditDto.PhoneNumber;
        CountryCode = SupplierEditDto.CountryCode;
        HasChanges = false;
    }

    protected override async Task Save()
    {
        SupplierUpdateDto supplierUpdateDto = new SupplierUpdateDto(
            _selectionStore.Id,
            Name,
            TimeOnly.FromDateTime(OpenAt.Value),
            TimeOnly.FromDateTime(CloseAt.Value),
            Street,
            City.Id,
            Number,
            CountryCode);

        Result result = await _mediator.Send(new Application.Commands.SupplierCommands.UpdateSupplierCommand(supplierUpdateDto));
        if(result.IsSuccess)
        {

            await _mediator.Publish(new Application.Notifications.SuccessNotification("تم تعديل بيانات المورد"));
            _messenger.Send(new Application.Messages.Suppliers.SupplierUpdatedMessage());
        }
        else
        {
            await _mediator.Publish(new Application.Notifications.ErrorNotification("حدث خطأ اثناء تعديل بيانات المورد"));
        }
    }
}
