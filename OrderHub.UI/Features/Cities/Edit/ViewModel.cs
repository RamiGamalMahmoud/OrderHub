using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Services;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CityDtos;

namespace OrderHub.UI.Features.Cities.Edit;

public partial class ViewModel : Editor.ViewModelBase, IParameterizedViewModel
{
    private readonly IMediator _mediator;
    private int _cityId;

    public ViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override string Title => "تعديل مدينة";

    public Task Initialize(object parameter)
    {
        if (parameter is not null && parameter is int id)
            _cityId = id;
        return Task.CompletedTask;
    }

    public async Task LoadAsync()
    {
        CityUpdateDto city = await _mediator.Send(new Application.Queries.CityQueries.GetCityForEditQuery(_cityId));
        if (city is null)
        {
            NotificationService.Instance.ShowError("تعذر تحميل بيانات المدينة.");
            OnRequestClose();
            return;
        }

        Name = city.Name;
        HasChanges = false;
    }

    protected override async Task Save()
    {
        Result result = await _mediator.Send(new Application.Commands.CityCommands.UpdateCityCommand(new CityUpdateDto(_cityId, Name)));
        if (result.IsSuccess)
        {
            NotificationService.Instance.ShowSuccess($"تم تحديث المدينة ({Name}) بنجاح.");
            OnRequestClose();
            WeakReferenceMessenger.Default.Send(new Application.Messages.Cities.CityUpdatedMessage());
            return;
        }

        NotificationService.Instance.ShowError(result.ErrorMessage);
    }
}
