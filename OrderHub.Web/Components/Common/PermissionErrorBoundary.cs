using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using OrderHub.Application.Authorization;

namespace OrderHub.Web.Components.Common;

public sealed class PermissionErrorBoundary : ErrorBoundary, IDisposable
{
    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    protected override void OnInitialized()
    {
        Navigation.LocationChanged += OnLocationChanged;
    }

    protected override Task OnErrorAsync(Exception exception)
    {
        if (exception is AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
            return Task.CompletedTask;
        }

        return base.OnErrorAsync(exception);
    }

    private void OnLocationChanged(
        object? sender,
        LocationChangedEventArgs args)
    {
        if (CurrentException is null)
        {
            return;
        }

        Recover();
    }

    public void Dispose()
    {
        Navigation.LocationChanged -= OnLocationChanged;
    }
}