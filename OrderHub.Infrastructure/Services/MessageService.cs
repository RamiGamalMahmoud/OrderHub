using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
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
    private readonly SemaphoreSlim _stateLock = new(1, 1);

    private readonly ConcurrentQueue<OutboxMessage> _queue = new();

    private CancellationTokenSource _cts;
    private Task _worker;

    public MessageService(
        AppDbContextFactory dbFactory,
        IMessageSender messageSender)
    {
        _dbFactory = dbFactory;
        _messageSender = messageSender;

        WeakReferenceMessenger.Default.Register<Application.Messages.Orders.MessagesCreatedMessage>(this, (r, m) => QueueMessages(m.OutboxMessages));
    }

    public void QueueMessage(OutboxMessage message)
    {
        if (message?.Recipient is null)
        {
            return;
        }

        _queue.Enqueue(message);
    }

    public void QueueMessages(IEnumerable<OutboxMessage> messages)
    {
        foreach (OutboxMessage message in messages.Where(message => message?.Recipient is not null))
        {
            WeakReferenceMessenger.Default.Send(new Application.Messages.Orders.AddingNewMessageToQueMessage(message.Recipient.Name));
            _queue.Enqueue(message);
        }
    }

    public async Task StartAsync()
    {
        await _stateLock.WaitAsync();

        try
        {
            if (_worker is { IsCompleted: false })
            {
                return;
            }

            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            await LoadMessagesAsync(_cts.Token);

            _worker = Task.Run(() => ProcessLoop(_cts.Token), _cts.Token);
        }
        finally
        {
            _stateLock.Release();
        }
    }

    private async Task LoadMessagesAsync(CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _dbFactory.CreateDbContext();

        IEnumerable<OutboxMessage> pendingMessages = await appDbContext.OutboxMessages
            .Where(x => x.Status == OutboxMessageStatus.Pending)
            .Include(x => x.Recipient)
            .ToListAsync(cancellationToken);

        QueueMessages(pendingMessages);
    }

    private async Task ProcessLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!_queue.TryDequeue(out OutboxMessage message))
            {
                try
                {
                    await Task.Delay(2000, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                continue;
            }

            try
            {
                bool sent = await SendMessageAsync(message);
                await UpdateMessageStatusAsync(message.Id, sent ? OutboxMessageStatus.Sent : OutboxMessageStatus.Failed, sent);
            }
            catch
            {
                await UpdateMessageStatusAsync(message.Id, OutboxMessageStatus.Failed, false);
            }
        }
    }

    private Task<bool> SendMessageAsync(OutboxMessage message)
    {
        if (message?.Recipient is null || string.IsNullOrWhiteSpace(message.Recipient.PhoneNumber))
        {
            return Task.FromResult(false);
        }

        return message.RecipientType switch
        {
            RecipientType.Supplier or RecipientType.Deliveryman
                => _messageSender.SendToGroupAsync(message.Recipient.PhoneNumber, message.Text),
            _ => _messageSender.SendToPhoneAsync(message.Recipient.PhoneNumber, message.Text)
        };
    }

    private async Task UpdateMessageStatusAsync(int messageId, OutboxMessageStatus status, bool sent)
    {
        using AppDbContext db = _dbFactory.CreateDbContext();

        OutboxMessage entity = await db.OutboxMessages.FindAsync(messageId);
        if (entity is null)
        {
            return;
        }

        entity.Status = status;
        entity.LastAttemptAt = DateTime.Now;
        entity.SentAt = sent ? DateTime.Now : null;

        await db.SaveChangesAsync();
        WeakReferenceMessenger.Default.Send(new Application.Messages.OutboxMessages.MessageStatusChangedMessage(
            entity.Id,
            entity.Status,
            entity.OrderId,
            entity.RecipientType));
    }

    public async Task StopAsync()
    {
        await _stateLock.WaitAsync();

        try
        {
            _cts?.Cancel();

            if (_worker is not null)
            {
                try
                {
                    await _worker;
                }
                catch (OperationCanceledException)
                {
                }
            }

            _worker = null;
            _cts?.Dispose();
            _cts = null;
        }
        finally
        {
            _stateLock.Release();
        }
    }
}
