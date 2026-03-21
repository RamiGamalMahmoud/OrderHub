using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services;

internal class MessageService : IMessageService
{
    private readonly AppDbContextFactory _dbFactory;
    private readonly IMessageSender _messageSender;
    private readonly IMessenger _messenger;

    private readonly ConcurrentQueue<OutboxMessage> _queue = new();

    private CancellationTokenSource _cts;
    private Task _worker;

    public MessageService(
        AppDbContextFactory dbFactory,
        IMessageSender messageSender,
        IMessenger messenger)
    {
        _dbFactory = dbFactory;
        _messageSender = messageSender;
        _messenger = messenger;

        WeakReferenceMessenger.Default.Register<Application.Messages.Orders.MessagesCreatedMessage>(this, (r, m) => QueueMessages(m.OutboxMessages));
    }

    public void QueueMessage(OutboxMessage message) => _queue.Enqueue(message);

    public void QueueMessages(IEnumerable<OutboxMessage> messages)
    {
        foreach (OutboxMessage message in messages)
        {
            _messenger.Send(new Application.Messages.Orders.AddingNewMessageToQueMessage(message.Recipient.Name));
            _queue.Enqueue(message);
        }
    }

    public async Task StartAsync()
    {
        if (_worker != null)
            return;

        _cts = new CancellationTokenSource();

        await LoadMessagesAsync();

        _worker = Task.Run(ProcessLoop);
    }

    private async Task LoadMessagesAsync()
    {
        using AppDbContext appDbContext = _dbFactory.CreateDbContext();

        IEnumerable<OutboxMessage> pendingMessages = await appDbContext.OutboxMessages
            .Where(x => x.Status == OutboxMessageStatus.Pending)
            .Include(x => x.Recipient)
            .ToListAsync();

        QueueMessages(pendingMessages);
    }

    private async Task ProcessLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            if (!_queue.TryDequeue(out OutboxMessage message))
            {
                await Task.Delay(2000);
                continue;
            }

            try
            {
                bool sent = await _messageSender.SendAsync(message.Recipient.PhoneNumber, message.Text);

                using AppDbContext db = _dbFactory.CreateDbContext();

                OutboxMessage entity = await db.OutboxMessages.FindAsync(message.Id);

                if (entity == null)
                    continue;

                entity.Status = sent ? OutboxMessageStatus.Sent : OutboxMessageStatus.Failed;
                entity.LastAttemptAt = System.DateTime.Now;
                entity.SentAt = sent ? System.DateTime.Now : null;

                await db.SaveChangesAsync();
                _messenger.Send(new Application.Messages.OutboxMessages.MessageStatusChangedMessage(message.Id, entity.Status, entity.OrderId, entity.RecipientType));
            }
            catch
            {
                using AppDbContext db = _dbFactory.CreateDbContext();

                OutboxMessage entity = await db.OutboxMessages.FindAsync(message.Id);

                if (entity != null)
                {
                    entity.Status = OutboxMessageStatus.Failed;
                    entity.LastAttemptAt = System.DateTime.Now;
                    await db.SaveChangesAsync();
                    _messenger.Send(new Application.Messages.OutboxMessages.MessageStatusChangedMessage(entity.Id, entity.Status, entity.OrderId, entity.RecipientType));
                }
            }
        }
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();

        if (_worker != null)
            await _worker;
    }
}
