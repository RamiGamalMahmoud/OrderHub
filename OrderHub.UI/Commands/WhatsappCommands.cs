using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrderHub.Application.Interfaces.Services;
using OrderHub.UI.Services;
using OrderHub.UI.States;
using System.Threading.Tasks;

namespace OrderHub.UI.Commands;

public partial class WhatsappCommands : ObservableObject, ICommandGroup
{
    private readonly IWhatsappService _whatsappService;
    private readonly IMessageService _messageService;
    private readonly MessagingState _state;

    public WhatsappCommands(
        IWhatsappService whatsappService,
        IMessageService messageService,
        MessagingState state)
    {
        _whatsappService = whatsappService;
        _messageService = messageService;
        _state = state;
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        bool started =
            await _whatsappService.StartWhatsAppAsync();

        if (!started)
        {
            NotificationService.Instance
                .ShowError("تعذر تشغيل واتساب ويب.");

            return;
        }

        _state.SetConnected(true);

        await _messageService.StartAsync();

        _state.SetMessageServiceRunning(true);

        NotificationService.Instance
            .ShowSuccess("تم تشغيل واتساب ويب وخدمة الرسائل.");
    }

    [RelayCommand]
    private async Task StopAsync()
    {
        if (_state.IsMessageServiceRunning)
        {
            await _messageService.StopAsync();

            _state.SetMessageServiceRunning(false);
        }

        if (_state.IsConnected)
        {
            _whatsappService.Stop();

            _state.SetConnected(false);
        }

        NotificationService.Instance
            .ShowSuccess("تم إيقاف واتساب ويب وخدمة الرسائل.");
    }

    [RelayCommand]
    private async Task RestartAsync()
    {
        await StopAsync();

        await OpenAsync();
    }

    [RelayCommand]
    private async Task StartMessagesServiceAsync()
    {
        if (!_state.IsConnected)
        {
            NotificationService.Instance
                .ShowError("يجب تشغيل واتساب ويب أولاً.");

            return;
        }

        if (_state.IsMessageServiceRunning)
            return;

        await _messageService.StartAsync();

        _state.SetMessageServiceRunning(true);
    }

    [RelayCommand]
    private async Task StopMessagesServiceAsync()
    {
        if (!_state.IsMessageServiceRunning)
            return;

        await _messageService.StopAsync();

        _state.SetMessageServiceRunning(false);
    }
}