using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Interfaces;
using OrderHub.UI.Stores.Markers;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CityDtos;

namespace OrderHub.UI.Features.Cities.Edit;

public partial class ViewModel : Editor.ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ISelectionStore<ICityMarker, int> _selectionStore;

    public ViewModel(IMediator mediator, ISelectionStore<ICityMarker, int> selectionStore)
    {
        _mediator = mediator;
        _selectionStore = selectionStore;
    }

    public override string Title => "تعديل مدينة";

    public async Task LoadAsync()
    {
        CityUpdateDto city = await _mediator.Send(new Application.Queries.CityQueries.GetCityForEditQuery(_selectionStore.Id));
        if (city is null)
        {
            await _mediator.Publish(new Application.Notifications.ErrorNotification("تعذر تحميل بيانات المدينة."));
            OnRequestClose();
            return;
        }

        Name = city.Name;
        HasChanges = false;
    }

    protected override async Task Save()
    {
        Result result = await _mediator.Send(new Application.Commands.CityCommands.UpdateCityCommand(new CityUpdateDto(_selectionStore.Id, Name)));
        if (result.IsSuccess)
        {
            await _mediator.Publish(new Application.Notifications.SuccessNotification($"تم تحديث المدينة ({Name}) بنجاح."));
            OnRequestClose();
            WeakReferenceMessenger.Default.Send(new Application.Messages.Cities.CityUpdatedMessage());
            return;
        }

        await _mediator.Publish(new Application.Notifications.ErrorNotification(result.ErrorMessage));
    }
}
