using CommunityToolkit.Mvvm.Input;
using OrderHub.Application.Common.Lookups;
using OrderHub.Application.Features.Messaging;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.UI.Helpers;
using OrderHub.UI.Services;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OrderHub.UI.Features.Messages.Index;

public partial class ViewModel
{
    [RelayCommand(CanExecute = nameof(HasSelectedMessage))]
    private void CopyMessage(
        OutboxMessageViewModel message)
    {
        StringBuilder builder = new();

        builder.AppendLine(message.Text);

        foreach (string note in message.Notes)
        {
            builder.AppendLine(note);
        }

        Clipboard.SetText(builder.ToString());

        NotificationService.Instance.Show(
            "تم نسخ الرسالة الى الحافظة");
    }

    [RelayCommand(CanExecute = nameof(HasSelectedMessage))]
    private async Task MarkMessageSent(
        OutboxMessageViewModel message)
    {
        Result<MarkMessageSentResponse> result =
            await _requestExecutor.ExecuteAsync(
                new MarkMessageSentCommand(
                    message.Id));

        if (!result.IsSuccess)
        {
            NotificationService.Instance.ShowError(
                result.ErrorMessage);

            return;
        }

        await ReloadAsync();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedMessage))]
    private async Task ResendMessage(
        OutboxMessageViewModel message)
    {
        bool confirmed =
            await DialogService.Instance.ConfirmAsync(
                "هل أنت متأكد أنك تريد إعادة إرسال هذه الرسالة؟");

        if (!confirmed)
            return;

        var requestMessage =
            new ResendOutboxMessageCommand.Message(
                message.Id,
                message.Text,
                message.Attachments
                    .Select(attachment =>
                        new ResendOutboxMessageCommand.AttachmentFile(
                            attachment.FilePath,
                            attachment.Name))
                    .ToList(),
                message.Notes);

        Result result =
            await _requestExecutor.ExecuteAsync(
                new ResendOutboxMessageCommand.Command(
                    requestMessage));

        if (!result.IsSuccess)
        {
            NotificationService.Instance.ShowError(
                result.ErrorMessage);

            return;
        }

        NotificationService.Instance.Show(
            "تمت إعادة جدولة الرسالة للإرسال.");

        await ReloadAsync();
    }

    [RelayCommand]
    private async Task SendMessage(
        OutboxMessageViewModel message)
    {
        if (message is null)
        {
            DialogService.Instance.Confirm(
                "يجب اختيار رسالة أولا.");

            return;
        }

        var requestMessage =
            new ResendOutboxMessageCommand.Message(
                message.Id,
                message.Text,
                message.Attachments
                    .Select(attachment =>
                        new ResendOutboxMessageCommand.AttachmentFile(
                            attachment.FilePath,
                            attachment.Name))
                    .ToList(),
                message.Notes);

        Result result =
            await _requestExecutor.ExecuteAsync(
                new ResendOutboxMessageCommand.Command(
                    requestMessage));

        if (!result.IsSuccess)
        {
            NotificationService.Instance.ShowError(
                result.ErrorMessage);

            return;
        }

        NotificationService.Instance.Show(
            "تمت إعادة جدولة الرسالة للإرسال.");

        await ReloadAsync();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedMessage))]
    private async Task SaveMessage()
    {
        if (SelectedMessage is null)
            return;

        var request =
            new SaveOutboxMessageAsNew.Message(
                SelectedMessage.Id,
                SelectedMessage.Text,
                [],
                SelectedMessage.Notes);

        Result result =
            await _requestExecutor.ExecuteAsync(
                new SaveOutboxMessageAsNew.Command(
                    request));

        if (!result.IsSuccess)
        {
            NotificationService.Instance.ShowError(
                result.ErrorMessage);

            return;
        }

        NotificationService.Instance.Show(
            "تم حفظ الرسالة كرسالة جديدة.");

        await ReloadAsync();
    }

    [RelayCommand]
    private async Task SendNewMessage()
    {
        await DialogService.Instance
            .ShowDialog<SendMessage.SendMessageView>(
                "إرسال رسالة جديدة");
    }

    // =========================
    // QR Code
    // =========================

    [RelayCommand(CanExecute = nameof(HasSelectedMessage))]
    private void GenerateMessageQrCode()
    {
        if (SelectedMessage is null)
            return;

        string message =
            string.Join(
                Environment.NewLine,
                new[] { SelectedMessage.Text }
                    .Concat(SelectedMessage.Notes));

        string whatsappUrl =
            $"https://wa.me/?text={Uri.EscapeDataString(message)}";

        var qrCode =
            QrCodeGenerator.Generate(
                whatsappUrl);

        DialogService.Instance.ShowDialog(
            new Services.Views.QrCodeViewer(qrCode),
            "إرسال رسالة واتساب");
    }
}