using OrderHub.Application.Interfaces.Services;
using System.Threading.Tasks;
using System.Windows;

namespace OrderHub.UI.StartUpSteps;

public class WhatsAppStartupStep : IStartupStep
{
    private readonly IWhatsappService _whatsapp;
    private readonly IMessageService _messageService;
    private readonly INotifier _notifier;

    public WhatsAppStartupStep(IWhatsappService whatsapp, IMessageService messageService, INotifier notifier)
    {
        _whatsapp = whatsapp;
        _messageService = messageService;
        _notifier = notifier;
    }

    public int Order => 3;
    public string DisplayName => "جاري تشغيل واتساب";

    public async Task ExecuteAsync()
    {

        bool started = await _whatsapp.StartWhatsAppAsync();
        if (!started)
        {
            await _notifier.Error("تعذر تشغيل واتساب ويب. لن يتم إرسال الرسائل حتى يتم فتحه بنجاح.");
            return;
        }

        await _messageService.StartAsync();
    }
}
