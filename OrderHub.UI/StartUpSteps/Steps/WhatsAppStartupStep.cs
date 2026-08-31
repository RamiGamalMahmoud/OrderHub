using OrderHub.Application.Interfaces.Services;
using OrderHub.UI.Services;
using System.Threading.Tasks;

namespace OrderHub.UI.StartUpSteps.Steps;

public class WhatsAppStartupStep : IStartupStep
{
    private readonly IWhatsappService _whatsapp;
    private readonly IMessageService _messageService;

    public WhatsAppStartupStep(IWhatsappService whatsapp, IMessageService messageService)
    {
        _whatsapp = whatsapp;
        _messageService = messageService;
    }

    public int Order => (int)StartUpdStepsOrder.WhatsAppStartup;
    public string DisplayName => "جاري تشغيل واتساب";

    public bool IsEnabled => false;

    public async Task ExecuteAsync()
    {

        bool started = await _whatsapp.StartWhatsAppAsync();
        if (!started)
        {
            NotificationService.Instance.ShowError("تعذر تشغيل واتساب ويب. لن يتم إرسال الرسائل حتى يتم فتحه بنجاح.");
            return;
        }

        await _messageService.StartAsync();
    }
}
