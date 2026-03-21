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
                    OrderNumber = outboxMessage.Order.OrderNumber,
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
                OrderNumber = m.Order.OrderNumber,
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

    partial void OnSearchTermChanged(string oldValue, string newValue) => ApplyFilter();

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

        OutboxMessages = new ObservableCollection<OutboxMessageViewModel>(
            filteredMessages.OrderByDescending(message => message.CreatedAt));
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
    public string PhoneNumber { get; init; }
    public DateTime CreatedAt { get; init; }
}
