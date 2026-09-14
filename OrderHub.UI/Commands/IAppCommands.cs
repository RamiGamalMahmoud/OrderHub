namespace OrderHub.UI.Commands;
public interface IAppCommands
{
    AuthCommands Auth { get; }
    NavigationCommands Navigation { get; }
    UpdateCommands Update { get; }
    SallaCommands Salla { get; }
    WhatsappCommands Whatsapp { get; }
    ApplicationCommands Application { get; }
}