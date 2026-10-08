using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderHub.Application.Interfaces;

namespace OrderHub.Web.Components.Pages.Features.Setup;

public partial class CreateAdmin
{
    [Inject]
    private IRequestExecutor RequestExecutor { get; set; } = null!;

    private MudForm? _form;

    private string? _displayName;
    private string? _userName;
    private string? _password;
    private string? _confirmPassword;

    private bool _showPassword;
    private bool _showConfirmPassword;
    private bool _isBusy;


    private void TogglePasswordVisibility()
    {
        _showPassword = !_showPassword;
    }

    private void ToggleConfirmPasswordVisibility()
    {
        _showConfirmPassword = !_showConfirmPassword;
    }

    private async Task CreateAdminAsync()
    {
        if (_form is null)
            return;

        await _form.ValidateAsync();

        if (!_form.IsValid)
            return;

        if (_password != _confirmPassword)
        {
            // هنضيف Validation مناسب هنا
            return;
        }

        _isBusy = true;
        await Task.Delay(3000);
        try
        {
            // await RequestExecutor.ExecuteAsync(
            //     new CreateAdminCommand(...));
        }
        finally
        {
            _isBusy = false;
        }
    }
}