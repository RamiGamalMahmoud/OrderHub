using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Services;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CityDtos;

namespace OrderHub.UI.Features.Cities.Create;

public partial class CreateCityViewModel : Editor.ViewModelBase
{
    private readonly IMediator _mediator;

    public CreateCityViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override string Title => "إضافة مدينة";

    protected override async Task Save()
    {
        Result result = await _mediator.Send(new Application.Commands.CityCommands.CreateCityCommand(new CityCreateDto(Name)));
        if (result.IsSuccess)
        {
            NotificationService.Instance.ShowSuccess($"تم إضافة مدينة ({Name}) بنجاح.");
            OnRequestClose();
            WeakReferenceMessenger.Default.Send(new Application.Messages.Cities.CityCreatedMessage());
        }
        else
        {
            NotificationService.Instance.ShowError(result.ErrorMessage);
        }
    }
}
