namespace OrderHub.UI.Commands;

public sealed class AppCommands : IAppCommands
{
    public AppCommands(
        AuthCommands auth,
        NavigationCommands navigation,
        UpdateCommands update,
        SallaCommands salla,
        WhatsappCommands whatsapp,
        ApplicationCommands application)
    {
        Auth = auth;
        Navigation = navigation;
        Update = update;
        Salla = salla;
        Whatsapp = whatsapp;
        Application = application;
    }

    public static IAppCommands Instance { get; private set; } = null!;

    public AuthCommands Auth { get; }
    public NavigationCommands Navigation { get; }
    public UpdateCommands Update { get; }
    public SallaCommands Salla { get; }
    public WhatsappCommands Whatsapp { get; }
    public ApplicationCommands Application { get; }

    public static void SetInstance(IAppCommands instance)
    {
        Instance ??= instance;
    }
}