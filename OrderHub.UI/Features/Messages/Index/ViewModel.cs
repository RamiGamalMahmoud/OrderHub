using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Messages.Index;

public partial class ViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    public ViewModel(IMediator mediator, IMessenger messenger)
    {
        _mediator = mediator;

        messenger.Register<Application.Messages.OutboxMessages.MessageStatusChangedMessage>(this, (r, m) =>
        {
            OutboxMessageViewModel outboxMessage = OutboxMessages.First(x => x.Id == m.Id);
            if (outboxMessage != null)
                outboxMessage.Status = new EnumItem<OutboxMessageStatus>(m.NewStatus, m.NewStatus.GetDescription());
        });

        messenger.Register<Application.Messages.Orders.MessagesCreatedMessage>(this, (r, m) =>
        {
            foreach (OutboxMessage outboxMessage in m.OutboxMessages)
            {
                OutboxMessages.Add(new OutboxMessageViewModel()
                {
                    Id = outboxMessage.Id,
                    Status = new EnumItem<OutboxMessageStatus>(outboxMessage.Status, outboxMessage.Status.GetDescription()),
                    RecipientName = outboxMessage.Recipient.Name,
                    RecipientType = new EnumItem<RecipientType>(outboxMessage.RecipientType, outboxMessage.RecipientType.GetDescription()),
                    OrderNumber = outboxMessage.Order.OrderNumber,
                    Text = outboxMessage.Text,
                    PhoneNumber = outboxMessage.Recipient.PhoneNumber
                });
            }
        });
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        OutboxMessages = new ObservableCollection<OutboxMessageViewModel>( (await _mediator.Send(new Application.Queries.OutboxMessageQueries.GetOutboxMessagesQuery()))
            .Select(m => new OutboxMessageViewModel()
            {
                Id = m.Id,
                Status = new EnumItem<OutboxMessageStatus>(m.Status, m.Status.GetDescription()),
                RecipientName = m.Recipient.Name,
                RecipientType = new EnumItem<RecipientType>(m.RecipientType, m.RecipientType.GetDescription()), // m.RecipientType,
                OrderNumber = m.Order.OrderNumber,
                Text = m.Text,
                PhoneNumber = m.Recipient.PhoneNumber
            })
            .ToList());
    }

    [ObservableProperty]
    private ObservableCollection<OutboxMessageViewModel> _outboxMessages;
}

public partial class OutboxMessageViewModel : ObservableObject
{
    public int Id { get; set; }

    [ObservableProperty]
    private EnumItem<OutboxMessageStatus> _status;

    public string OrderNumber { get; init; }
    public string RecipientName { get; init; }
    public EnumItem<RecipientType> RecipientType { get; init; }
    public string Text { get; init; }
    public string PhoneNumber { get; init; }
}
