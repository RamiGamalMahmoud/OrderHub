using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OrderHub.Application.Common.Extensions;
using OrderHub.Application.Common.Lookups;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Enums;
using OrderHub.UI.Common;
using OrderHub.UI.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Messages.SendMessage;

public partial class SendMessageViewModel : ObservableObject, IParameterizedViewModel
{
    private readonly IRequestExecutor _requestExecutor;

    public SendMessageViewModel(IRequestExecutor requestExecutor)
    {
        AttachedFiles.CollectionChanged += AttachedFiles_CollectionChanged;
        _requestExecutor = requestExecutor;
    }

    private void AttachedFiles_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        SendCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private string _message;

    [ObservableProperty]
    private string _caption;

    [ObservableProperty]
    private IEnumerable<Recipient> _recipients;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private Recipient _selectedRecipient;

    public ObservableCollection<MessageAttachmentItem> AttachedFiles { get; } = [];

    [RelayCommand]
    private void AddAttachment()
    {
        var openFileDialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "كل الملفات المدعومة (*.jpg;*.jpeg;*.png;*.pdf;*.docx)|*.jpg;*.jpeg;*.png;*.pdf;*.docx|" +
                     "الصور (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|" +
                     "المستندات (*.pdf;*.docx)|*.pdf;*.docx"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            foreach (var filePath in openFileDialog.FileNames)
            {
                if (!AttachedFiles.Any(f => f.FilePath == filePath))
                {
                    AttachedFiles.Add(new MessageAttachmentItem(filePath));
                }
            }
        }
    }

    public async Task LoadAsync()
    {
        var recipients = await _requestExecutor.ExecuteAsync(new Application.Features.Messaging.GetAllRecipients.Query());
        Recipients = recipients.Select(recipient => new Recipient(
            recipient.RecipientId,
            recipient.RecipientName,
            recipient.Destination,
            new EnumItem<RecipientType>(recipient.RecipientType, recipient.RecipientType.GetDescription())));
    }

    [RelayCommand]
    private void RemoveAttachment(MessageAttachmentItem item)
    {
        if (item != null && AttachedFiles.Contains(item))
        {
            AttachedFiles.Remove(item);
        }
    }

    private bool CanSendMessage =>
        (!string.IsNullOrWhiteSpace(Message)
        || AttachedFiles.Count > 0)
        && SelectedRecipient is not null
        && !string.IsNullOrWhiteSpace(SelectedRecipient.Destination);

    [RelayCommand(CanExecute = nameof(CanSendMessage))]
    private async Task Send()
    {
        var filePaths = AttachedFiles.Select(f => f.FilePath).ToArray();

        var messageToSend = new MessageToSend(Message, filePaths);
        Application.Features.Messaging.SendMessageCommand.Request request = new Application.Features.Messaging.SendMessageCommand.Request(
            SelectedRecipient.RecipientId,
            SelectedRecipient.RecipientName,
            SelectedRecipient.Destination,
            SelectedRecipient.RecipientType.Value,
            new MessageToSend(Message, filePaths));

        var result = await _requestExecutor.ExecuteAsync(new Application.Features.Messaging.SendMessageCommand.Command(request));

        if (result.IsSuccess)
        {
            NotificationService.Instance.ShowSuccess("تم قيد الرسالة للارسال");

            Message = string.Empty;
            Caption = string.Empty;
            AttachedFiles.Clear();
            SelectedRecipient = null;
        }
        else
        {
            NotificationService.Instance.ShowError("لم يتم ارسال الرسالة");
        }
    }

    public async Task Initialize(object parameter)
    {
        if (parameter is null)
            return;

        if (parameter is not MessageData data)
            throw new System.ArgumentException(null, nameof(parameter));

        Message = data.Text;
        AttachedFiles.Add(new MessageAttachmentItem(data.Attachments[0]));
        await LoadAsync();
        SelectedRecipient = Recipients.Where(recipient => recipient.Destination == data.Destination).FirstOrDefault();
    }
}

public record MessageData(string Text, string Destination, IReadOnlyList<string> Attachments);

public record Recipient(int RecipientId, string RecipientName, string Destination, EnumItem<RecipientType> RecipientType);