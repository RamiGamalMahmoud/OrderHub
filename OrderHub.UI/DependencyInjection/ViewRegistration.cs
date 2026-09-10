using Microsoft.Extensions.DependencyInjection;

namespace OrderHub.UI.DependencyInjection;
internal static class ViewRegistration
{
    public static IServiceCollection AddViews(this IServiceCollection services)
    {
        services.AddSingleton<Features.NotFoundView>();
        services.AddSingleton<Features.MainWindow.MainWindowView>();
        services.AddTransient<Features.Settings.ClientCredentialsSettings.ClientCredentialsSettingsView>();
        services.AddSingleton<Features.Home.HomeView>();
        services.AddSingleton<Features.Splash>();

        // Dashboard
        services.AddSingleton<Features.Dashboard.DashboardView>();

        // Suppliers
        services.AddSingleton<Features.Suppliers.Index.View>();
        services.AddTransient<Features.Suppliers.Create.View>();
        services.AddTransient<Features.Suppliers.Update.View>();

        // Cities
        services.AddTransient<Features.Cities.Create.CreateCityView>();
        services.AddTransient<Features.Cities.Edit.EditCityView>();

        // Clients
        services.AddSingleton<Features.Clients.Index.View>();
        services.AddTransient<Features.Clients.Create.View>();
        services.AddTransient<Features.Clients.Update.View>();

        // Categories
        services.AddSingleton<Features.Categories.Index.View>();
        services.AddTransient<Features.Categories.Create.View>();
        services.AddTransient<Features.Categories.Update.View>();

        // Cities
        services.AddSingleton<Features.Cities.Index.View>();

        // Products
        services.AddSingleton<Features.Products.Index.View>();
        services.AddTransient<Features.Products.Create.View>();
        services.AddTransient<Features.Products.Update.View>();

        // Deliverymen
        services.AddSingleton<Features.Deliverymen.Index.View>();
        services.AddTransient<Features.Deliverymen.Create.View>();
        services.AddTransient<Features.Deliverymen.Edit.View>();

        // ShippingCarriers
        services.AddSingleton<Features.ShippingCarriers.Index.View>();
        services.AddTransient<Features.ShippingCarriers.Create.View>();
        services.AddTransient<Features.ShippingCarriers.Edit.View>();

        // Orders
        services.AddSingleton<Features.Orders.Index.View>();
        services.AddTransient<Features.Orders.Create.View>();
        services.AddTransient<Features.Orders.Edit.View>();

        // Messages
        services.AddSingleton<Features.Messages.Index.View>();
        services.AddTransient<Features.Messages.SendMessage.SendMessageView>();

        // Documents
        services.AddTransient<Features.Documents.Preview.PreviewDocumentView>();

        // Whatsapp
        services.AddSingleton<Features.WhatsappGroups.Index.View>();
        services.AddTransient<Features.WhatsappGroups.Create.View>();
        services.AddTransient<Features.WhatsappGroups.Update.View>();

        // Settings
        services.AddTransient<Features.Settings.Home.SettingsHomeView>();
        services.AddSingleton<Features.Settings.Properties.PropertiesView>();

        // Database Configuration
        services.AddTransient<Features.DatabaseConfiguration.DatabaseConfigurationView>();

        // Authentication
        services.AddTransient<Features.Authentication.ApiLogin.ApiLoginView>();
        services.AddTransient<Features.Authentication.DatabaseLogin.DatabaseLoginView>();

        // Administration
        services.AddTransient<Features.Administration.Admin.CreateAdmin.CreateAdminView>();

        return services;
    }
}
