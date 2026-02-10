using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CityDtos;

namespace OrderHub.UI.Features.Cities.Create;

public partial class CreateCityViewModel : ObservableValidator
{
    private readonly IMediator _mediator;
    private readonly IMessenger _messenger;

    public CreateCityViewModel(IMediator mediator, IMessenger messenger)
    {
        _mediator = mediator;
        _messenger = messenger;
        ValidateAllProperties();
    }

    [ObservableProperty]
    [Required(ErrorMessage = "اسم المدينة مطلوب")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = string.Empty;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        await _mediator.Send(new Application.Commands.CityCommands.CreateCityCommand(new CityCreateDto(Name)));
        _messenger.Send(new Application.Messages.Cities.CityCreatedMessage());
    }

    private bool CanSave => !string.IsNullOrWhiteSpace(Name);
}
