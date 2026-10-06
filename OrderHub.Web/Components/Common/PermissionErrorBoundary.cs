using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OrderHub.Application.Authorization;

namespace OrderHub.Web.Components.Common;

public sealed class PermissionErrorBoundary : ErrorBoundary
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    protected override Task OnErrorAsync(Exception exception)
    {
        if (exception is AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
            return Task.CompletedTask;
        }
        return base.OnErrorAsync(exception);
    }
}
