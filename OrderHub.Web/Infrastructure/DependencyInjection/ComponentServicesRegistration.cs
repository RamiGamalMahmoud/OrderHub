using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using OrderHub.Web.Components.Pages.Features.Orders;
using OrderHub.Web.Services;

namespace OrderHub.Web.Infrastructure.DependencyInjection;

internal static class ComponentServicesRegistration
{
    public static IServiceCollection AddComponents(this IServiceCollection services)
    {
        services.AddScoped<OrdersListState>();

        services
            .AddRazorComponents()
            .AddInteractiveServerComponents();

        services.AddMudServices();
        services.AddCascadingAuthenticationState();
        services.AddEditorServices();

        return services;
    }
}
