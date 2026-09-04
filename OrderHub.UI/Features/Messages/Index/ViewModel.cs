using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OrderHub.Application.Common.Extensions;
using OrderHub.Application.Common.Lookups;
using OrderHub.Application.Features.Messaging;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Application.Queries;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.UI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Messages.Index;

public partial class ViewModel : ObservableObject
{
    private readonly IRequestExecutor _requestExecutor;
    private readonly IApplicationDirectoriesService _directoriesService;
    private List<OutboxMessageViewModel> _allOutboxMessages = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedMessage))]
    [NotifyCanExecuteChangedFor(nameof(ResendMessageCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteMessageCommand))]
    private OutboxMessageViewModel _selectedMessage;
    public bool HasSelectedMessage => SelectedMessage is not null;

    [ObservableProperty]
    private ObservableCollection<MessageSummaryItemViewModel> _statusSummaries = [];

    public ViewModel(IRequestExecutor requestExecutor, IApplicationDirectoriesService directoriesService)
    {
        _requestExecutor = requestExecutor;

        WeakReferenceMessenger.Default.Register<Application.Messages.OutboxMessages.MessageStatusChangedMessage>(this, (r, m) =>
        {
            OutboxMessageViewModel outboxMessage = _allOutboxMessages.FirstOrDefault(x => x.Id == m.Id);
            if (outboxMessage != null)
            {
                outboxMessage.Status = new EnumItem<OutboxMessageStatus>(m.NewStatus, m.NewStatus.GetDescription());
            }

            ApplyFilter();
        });

        WeakReferenceMessenger.Default.Register<Application.Messages.Orders.MessagesCreatedMessage>(this, (r, m) =>
        {
            foreach (OutboxMessage outboxMessage in m.OutboxMessages)
            {
                _allOutboxMessages.Insert(0, new OutboxMessageViewModel()
                {
                    Id = outboxMessage.Id,
                    Status = new EnumItem<OutboxMessageStatus>(outboxMessage.Status, outboxMessage.Status.GetDescription()),
                    RecipientName = outboxMessage.Recipient.Name,
                    RecipientType = new EnumItem<RecipientType>(outboxMessage.RecipientType, outboxMessage.RecipientType.GetDescription()),
                    OrderNumber = GetDisplayTitle(outboxMessage),
                    Text = outboxMessage.Text,
                    PhoneNumber = outboxMessage.Recipient.PhoneNumber,
                    CreatedAt = outboxMessage.CreatedAt,
                    LastAttemptAt = outboxMessage.LastAttemptAt,
                });
            }

            ApplyFilter();
        });
        _directoriesService = directoriesService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IEnumerable<OutboxMessageQueries.OutboxMessageListItem> outboxMessages = await _requestExecutor.ExecuteAsync(new OutboxMessageQueries.GetOutboxMessagesQuery());

        _allOutboxMessages = outboxMessages
            .Select(m =>
            {
                OutboxMessageViewModel messageViewModel = new OutboxMessageViewModel()
                {
                    Id = m.Id,
                    Status = new EnumItem<OutboxMessageStatus>(m.Status, m.Status.GetDescription()),
                    RecipientName = m.RecipientName,
                    RecipientType = new EnumItem<RecipientType>(m.RecipientType, m.RecipientType.GetDescription()),
                    OrderNumber = m.OrderNumber,
                    Text = m.Text,
                    PhoneNumber = m.PhoneNumber,
                    CreatedAt = m.CreatedAt,
                    LastAttemptAt = m.LastAttemptAt,
                };
                messageViewModel
                    .AddAttachments(
                        m.Attachments?
                            .ToDictionary(x => Path.Combine( _directoriesService.AttachmentsDirectory, x.StoredName), x => x.OriginalName));

                messageViewModel.AddNotes(m.Notes?.ToList());
                return messageViewModel;
            })
            .OrderByDescending(m => m.CreatedAt)
            .ToList();

        ApplyFilter();
    }

    [ObservableProperty]
    private ObservableCollection<OutboxMessageViewModel> _outboxMessages;

    [ObservableProperty]
    private string _searchTerm;

    [ObservableProperty]
    private EnumItem<OutboxMessageStatus> _selectedStatusFilter;

    [ObservableProperty]
    private DateTime? _fromDate = DateTime.Today;

    [ObservableProperty]
    private DateTime? _toDate = DateTime.Today;

    public IEnumerable<EnumItem<OutboxMessageStatus>> StatusFilters =>
        new[] { new EnumItem<OutboxMessageStatus>(default, "الكل") }
        .Concat(EnumItems.For<OutboxMessageStatus>());

    partial void OnSearchTermChanged(string oldValue, string newValue) => ApplyFilter();

    partial void OnSelectedStatusFilterChanged(EnumItem<OutboxMessageStatus> oldValue, EnumItem<OutboxMessageStatus> newValue) => ApplyFilter();

    partial void OnFromDateChanged(DateTime? oldValue, DateTime? newValue) => ApplyFilter();

    partial void OnToDateChanged(DateTime? oldValue, DateTime? newValue) => ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<OutboxMessageViewModel> filteredMessages = _allOutboxMessages;

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            string term = SearchTerm.Trim();

            filteredMessages = filteredMessages.Where(message =>
                message.OrderNumber?.Contains(term) == true
                || message.RecipientName?.Contains(term) == true
                || message.PhoneNumber?.Contains(term) == true
                || message.Text?.Contains(term) == true
                || message.RecipientType?.DisplayName?.Contains(term) == true);
        }

        if (SelectedStatusFilter is not null && SelectedStatusFilter.DisplayName != "الكل")
        {
            filteredMessages = filteredMessages.Where(message => message.Status?.Value.Equals(SelectedStatusFilter.Value) == true);
        }

        if (FromDate.HasValue)
        {
            filteredMessages = filteredMessages.Where(message => message.CreatedAt >= FromDate.Value.Date);
        }

        if (ToDate.HasValue)
        {
            filteredMessages = filteredMessages.Where(message => message.CreatedAt < ToDate.Value.Date.AddDays(1));
        }

        List<OutboxMessageViewModel> materializedMessages = filteredMessages
            .OrderByDescending(message => message.CreatedAt)
            .ToList();

        OutboxMessages = new ObservableCollection<OutboxMessageViewModel>(
            materializedMessages);

        UpdateSummary(materializedMessages);
    }

    private void UpdateSummary(IEnumerable<OutboxMessageViewModel> filteredMessages)
    {
        Dictionary<OutboxMessageStatus, int> counts = filteredMessages
            .GroupBy(message => message.Status.Value)
            .ToDictionary(group => group.Key, group => group.Count());

        ObservableCollection<MessageSummaryItemViewModel> statusSummaries = [];
        foreach (OutboxMessageStatus status in Enum.GetValues<OutboxMessageStatus>())
        {
            statusSummaries.Add(new MessageSummaryItemViewModel(
                status.GetDescription(),
                counts.GetValueOrDefault(status, 0)));
        }

        System.Windows.Application.Current.Dispatcher.InvokeAsync(() => StatusSummaries = statusSummaries);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedMessage))]
    private async Task ResendMessage(OutboxMessageViewModel message)
    {
        if (!await DialogService.Instance.ConfirmAsync("هل أنت متأكد أنك تريد إعادة إرسال هذه الرسالة؟"))
        {
            return;
        }
        ResendOutboxMessageCommand.Message requestMessage = new ResendOutboxMessageCommand.Message(
            message.Id, 
            message.Text,
            message
                .Attachments
                .Select(x => new ResendOutboxMessageCommand.AttachmentFile(x.FilePath, x.Name))
                .ToList(),
            message.Notes);

        Result result = await _requestExecutor.ExecuteAsync(new ResendOutboxMessageCommand.Command(requestMessage));

        if (!result.IsSuccess)
        {
            NotificationService.Instance.ShowSuccess(result.ErrorMessage);
            return;
        }

        NotificationService.Instance.Show("تمت إعادة جدولة الرسالة للإرسال.");
        message.MarkIsSending();
    }

    [RelayCommand]
    private async Task SendMessage(OutboxMessageViewModel message)
    {
        if(message is null)
        {
            DialogService.Instance.Confirm("يجب اختيار رسالة أولا.");
            return;
        }
        ResendOutboxMessageCommand.Message requestMessage = new ResendOutboxMessageCommand.Message(
            message.Id,
            message.Text,
            message
                .Attachments
                .Select(x => new ResendOutboxMessageCommand.AttachmentFile(x.FilePath, x.Name))
                .ToList(),
            message.Notes);

        Result result = await _requestExecutor.ExecuteAsync(new ResendOutboxMessageCommand.Command(requestMessage));

        if (!result.IsSuccess)
        {
            message.MarkIsSending();
            return;
        }
        
        message.Status = new EnumItem<OutboxMessageStatus>(OutboxMessageStatus.Sending, OutboxMessageStatus.Sending.GetDescription());
        NotificationService.Instance.Show("تمت إعادة جدولة الرسالة للإرسال.");
    }

    [RelayCommand(CanExecute = nameof(HasSelectedMessage))]
    private Task CancelSendingMessage(OutboxMessageViewModel message)
    {
        if (message is null)
        {
            DialogService.Instance.Confirm("يجب اختيار رسالة أولا.");
            return Task.CompletedTask;
        }
        message.CancelSending();
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedMessage))]
    private async Task DeleteMessage(OutboxMessageViewModel message)
    {
        if (!await DialogService.Instance.ConfirmAsync("هل أنت متأكد أنك تريد حذف هذه الرسالة؟"))
        {
            return;
        }

        if (message is null)
        {
            return;
        }

        if (!await DialogService.Instance.ConfirmAsync($"هل تريد حذف الرسالة للمستلم ({message.RecipientName})؟"))
        {
            return;
        }

        Result result = await _requestExecutor.ExecuteAsync(new Application.Features.Messaging.DeleteOutboxMessage.Command(message.Id));

        if (!result.IsSuccess)
        {
            NotificationService.Instance.Show(result.ErrorMessage);
            return;
        }

        NotificationService.Instance.ShowSuccess("تم حذف الرسالة بنجاح.");
        OutboxMessages.Remove(message);
    }

    [RelayCommand]
    private async Task SendNewMessage()
    {
        await DialogService.Instance.ShowDialog<SendMessage.SendMessageView>("إرسال رسالة جديدة");
    }

    [RelayCommand]
    private void SetToday()
    {
        FromDate = DateTime.Today;
        ToDate = DateTime.Today;
    }

    [RelayCommand]
    private void SetCurrentWeek()
    {
        DateTime today = DateTime.Today;
        int diff = ((int)today.DayOfWeek + 6) % 7;
        DateTime startOfWeek = today.AddDays(-diff);

        FromDate = startOfWeek;
        ToDate = startOfWeek.AddDays(6);
    }

    [RelayCommand]
    private void SetCurrentMonth()
    {
        DateTime today = DateTime.Today;
        DateTime startOfMonth = new(today.Year, today.Month, 1);

        FromDate = startOfMonth;
        ToDate = startOfMonth.AddMonths(1).AddDays(-1);
    }

    private static string GetDisplayTitle(OutboxMessage outboxMessage)
    {
        if (outboxMessage.Order is null)
        {
            return string.Empty;
        }

        int? entityId = outboxMessage.Recipient switch
        {
            ClientRecipient clientRecipient => clientRecipient.ClientId,
            SupplierRecipient supplierRecipient => supplierRecipient.SupplierId,
            DeliverymanRecipient deliverymanRecipient => deliverymanRecipient.DeliveryManId,
            _ => null
        };

        if (outboxMessage.RecipientType == RecipientType.ShippingCarrier)
        {
            return outboxMessage.Order.GetDisplayTitle(RecipientType.Client, outboxMessage.Order.ClientId);
        }

        return outboxMessage.Order.GetDisplayTitle(outboxMessage.RecipientType, entityId);
    }
}

public sealed class MessageSummaryItemViewModel
{
    public MessageSummaryItemViewModel(string title, int count)
    {
        Title = title;
        Count = count;
    }

    public string Title { get; }

    public int Count { get; }

    public string DisplayText => $"{Title} - {Count}";
}
