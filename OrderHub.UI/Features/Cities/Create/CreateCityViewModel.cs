using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CityDtos;

namespace OrderHub.UI.Features.Cities.Create;

public partial class CreateCityViewModel : Editor.ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IMessenger _messenger;

    public CreateCityViewModel(IMediator mediator, IMessenger messenger)
    {
        _mediator = mediator;
        _messenger = messenger;
    }

    public override string Title => "إضافة مدينة";

    protected override async Task Save()
    {
        Result result = await _mediator.Send(new Application.Commands.CityCommands.CreateCityCommand(new CityCreateDto(Name)));
        if (result.IsSuccess)
        {
            await _mediator.Publish(new Application.Notifications.SuccessNotification($"تم إضافة مدينة ({Name}) بنجاح."));
            OnRequestClose();
            _messenger.Send(new Application.Messages.Cities.CityCreatedMessage());
        }
        else
        {
            await _mediator.Publish(new Application.Notifications.ErrorNotification(result.ErrorMessage));
        }
    }
}
