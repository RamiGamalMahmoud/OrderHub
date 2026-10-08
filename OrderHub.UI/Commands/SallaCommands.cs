using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using OrderHub.UI.States;
using System.Threading.Tasks;

namespace OrderHub.UI.Commands;
public partial class SallaCommands : ObservableObject, ICommandGroup
{
    private readonly IMediator _mediator;
    private readonly SallaState _sallaState;

    public SallaCommands(IMediator mediator, SallaState sallaState)
    {
        _mediator = mediator;
        _sallaState = sallaState;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        _sallaState.IsAuthenticated = await _mediator.Send(new Application.Commands.AuthCommand());

        if (_sallaState.IsAuthenticated)
            return;

        await DialogService.Instance
            .ShowDialog<Features.Settings.ClientCredentialsSettings.ClientCredentialsSettingsView>(
                "إعدادات الاتصال بسلة");
    }
}