using OrderHub.Application.Interfaces.Services;
using System.Threading.Tasks;

namespace OrderHub.UI.StartUpSteps;

public class WhatsAppStartupStep : IStartupStep
{
    private readonly IWhatsappService _whatsapp;

    public WhatsAppStartupStep(IWhatsappService whatsapp)
    {
        _whatsapp = whatsapp;
    }

    public int Order => 3;

    public async Task ExecuteAsync()
    {
        await _whatsapp.StartWhatsAppAsync();
    }
}