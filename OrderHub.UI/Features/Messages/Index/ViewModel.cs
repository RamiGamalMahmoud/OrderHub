using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Messages.Index;

public partial class ViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private List<OutboxMessageViewModel> _allOutboxMessages = [];

    [ObservableProperty]
    private ObservableCollection<MessageSummaryItemViewModel> _statusSummaries = [];

    public ViewModel(IMediator mediator, IMessenger messenger)
    {
        _mediator = mediator;

        messenger.Register<Application.Messages.OutboxMessages.MessageStatusChangedMessage>(this, (r, m) =>
        {
            OutboxMessageViewModel outboxMessage = _allOutboxMessages.FirstOrDefault(x => x.Id == m.Id);
            if (outboxMessage != null)
            {
                outboxMessage.Status = new EnumItem<OutboxMessageStatus>(m.NewStatus, m.NewStatus.GetDescription());
            }

            ApplyFilter();
        });

        messenger.Register<Application.Messages.Orders.MessagesCreatedMessage>(this, (r, m) =>
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
                    CreatedAt = outboxMessage.CreatedAt
                });
            }

            ApplyFilter();
        });
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        _allOutboxMessages = (await _mediator.Send(new Application.Queries.OutboxMessageQueries.GetOutboxMessagesQuery()))
            .Select(m => new OutboxMessageViewModel()
            {
                Id = m.Id,
                Status = new EnumItem<OutboxMessageStatus>(m.Status, m.Status.GetDescription()),
                RecipientName = m.Recipient.Name,
                RecipientType = new EnumItem<RecipientType>(m.RecipientType, m.RecipientType.GetDescription()),
                OrderNumber = GetDisplayTitle(m),
                Text = m.Text,
                PhoneNumber = m.Recipient.PhoneNumber,
                CreatedAt = m.CreatedAt
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
        .Concat(Enum.GetValues(typeof(OutboxMessageStatus))
            .Cast<OutboxMessageStatus>()
            .Select(status => new EnumItem<OutboxMessageStatus>(status, status.GetDescription())));

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

    [RelayCommand]
    private async Task ResendMessage(OutboxMessageViewModel message)
    {
        Result result = await _mediator.Send(new Application.Commands.OutboxMessageCommands.ResendOutboxMessageCommand(message.Id));

        if (!result.IsSuccess)
        {
            await _mediator.Publish(new Application.Notifications.ErrorNotification(result.ErrorMessage));
            return;
        }

        await _mediator.Publish(new Application.Notifications.SuccessNotification("تمت إعادة جدولة الرسالة للإرسال."));
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

public partial class OutboxMessageViewModel : ObservableObject
{
    public int Id { get; set; }

    [ObservableProperty]
    private EnumItem<OutboxMessageStatus> _status;

    public bool CanResend => Status?.Value == OutboxMessageStatus.Failed;

    partial void OnStatusChanged(EnumItem<OutboxMessageStatus> oldValue, EnumItem<OutboxMessageStatus> newValue)
        => OnPropertyChanged(nameof(CanResend));

    public string OrderNumber { get; init; }
    public string RecipientName { get; init; }
    public EnumItem<RecipientType> RecipientType { get; init; }
    public string Text { get; init; }
    public string Title => Text?.Split("\n").FirstOrDefault() ?? string.Empty;
    public string PhoneNumber { get; init; }
    public DateTime CreatedAt { get; init; }
}
