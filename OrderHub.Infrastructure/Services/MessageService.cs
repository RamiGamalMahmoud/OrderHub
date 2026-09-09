using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services;

internal class MessageService : IMessageService
{
    private readonly AppDbContextFactory _dbFactory;
    private readonly IApplicationDirectoriesService _directoriesService;
    private readonly IAppLogger _logger;
    private readonly IMessageSender _messageSender;
    private readonly MessageSendingOptions _options;

    private readonly SemaphoreSlim _stateLock = new(1, 1);
    private readonly ConcurrentQueue<OutboxMessage> _queue = new();

    private CancellationTokenSource _cts;
    private Task _worker;

    public MessageService(
        AppDbContextFactory dbFactory,
        IMessageSender messageSender,
        IApplicationDirectoriesService directoriesService,
        IAppLogger logger,
        MessageSendingOptions options)
    {
        _dbFactory = dbFactory;
        _messageSender = messageSender;
        _directoriesService = directoriesService;
        _logger = logger;
        _options = options;
    }

    public void QueueMessage(OutboxMessage message)
    {
        if (message?.Recipient is null)
        {
            _ = _logger.LogInfoAsync(
                "[QUEUE] Message ignored because recipient is missing.");

            return;
        }

        _queue.Enqueue(message);

        _ = _logger.LogInfoAsync(
            $"[QUEUE] Message {message.Id} queued. " +
            $"RecipientType={message.RecipientType}, " +
            $"Phone={message.Recipient.PhoneNumber}, " +
            $"QueueSize={_queue.Count}");
    }

    public void QueueMessages(IEnumerable<OutboxMessage> messages)
    {
        foreach (OutboxMessage message in messages)
        {
            if (message?.Recipient is null)
            {
                _ = _logger.LogInfoAsync(
                    "[QUEUE] Message ignored because recipient is missing.");

                continue;
            }

            _queue.Enqueue(message);

            _ = _logger.LogInfoAsync(
                $"[QUEUE] Message {message.Id} queued. " +
                $"RecipientType={message.RecipientType}, " +
                $"Phone={message.Recipient.PhoneNumber}, " +
                $"QueueSize={_queue.Count}");
        }
    }

    public async Task StartAsync()
    {
        await _stateLock.WaitAsync();

        try
        {
            if (_worker is { IsCompleted: false })
            {
                await _logger.LogInfoAsync(
                    "[SERVICE] Start requested but message worker is already running.");

                return;
            }

            await _logger.LogInfoAsync(
                "[SERVICE] Starting message service.");

            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            await LoadMessagesAsync(_cts.Token);

            await _logger.LogInfoAsync(
                $"[SERVICE] Message service started. QueueSize={_queue.Count}");

            _worker = Task.Run(
                () => ProcessLoop(_cts.Token),
                _cts.Token);
        }
        finally
        {
            _stateLock.Release();
        }
    }

    private async Task LoadMessagesAsync(
        CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _dbFactory.CreateDbContext();

        IEnumerable<OutboxMessage> pendingMessages =
            await appDbContext.OutboxMessages
                .Where(x => x.Status == OutboxMessageStatus.Sending)
                .Include(x => x.Recipient)
                .ToListAsync(cancellationToken);

        await _logger.LogInfoAsync(
            $"[LOAD] Loaded {pendingMessages.Count()} pending message(s) from database.");

        QueueMessages(pendingMessages);
    }

    private async Task ProcessLoop(
        CancellationToken cancellationToken)
    {
        int messagesInCurrentBatch = 0;
        int currentBatchSize = GetBatchSize();

        await _logger.LogInfoAsync(
            $"[BATCH] New batch started. TargetSize={currentBatchSize}");

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!_queue.TryDequeue(out OutboxMessage message))
            {
                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(2),
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                continue;
            }

            DateTime processingStartedAt = DateTime.Now;

            await _logger.LogInfoAsync(
                $"[PROCESS] Message {message.Id} dequeued at " +
                $"{processingStartedAt:yyyy-MM-dd HH:mm:ss.fff}. " +
                $"QueueSize={_queue.Count}, " +
                $"BatchProgress={messagesInCurrentBatch + 1}/{currentBatchSize}");

            bool sent = false;

            try
            {
                await _logger.LogInfoAsync(
                    $"[SEND] Message {message.Id} sending. " +
                    $"RecipientType={message.RecipientType}, " +
                    $"Phone={message.Recipient?.PhoneNumber}");

                sent = await SendMessageAsync(message);

                await _logger.LogInfoAsync(
                    $"[SEND] Message {message.Id} completed. " +
                    $"Result={(sent ? "SUCCESS" : "FAILED")}");

                await UpdateMessageStatusAsync(
                    message.Id,
                    sent
                        ? OutboxMessageStatus.Sent
                        : OutboxMessageStatus.Failed,
                    sent);
            }
            catch (Exception exception)
            {
                await _logger.LogErrorAsync(
                    $"[SEND] Message {message.Id} failed with exception.",
                    exception);

                try
                {
                    await UpdateMessageStatusAsync(
                        message.Id,
                        OutboxMessageStatus.Failed,
                        false);
                }
                catch (Exception statusException)
                {
                    await _logger.LogErrorAsync(
                        $"[STATUS] Failed to update Message {message.Id} status to Failed.",
                        statusException);
                }
            }

            messagesInCurrentBatch++;

            bool batchCompleted =
                messagesInCurrentBatch >= currentBatchSize;

            if (batchCompleted)
            {
                await HandleBatchCompletedAsync(
                    messagesInCurrentBatch,
                    currentBatchSize,
                    cancellationToken);

                messagesInCurrentBatch = 0;
                currentBatchSize = GetBatchSize();

                await _logger.LogInfoAsync(
                    $"[BATCH] New batch started. " +
                    $"TargetSize={currentBatchSize}");

                continue;
            }

            TimeSpan delay = GetMessageDelay();

            await _logger.LogInfoAsync(
                $"[DELAY] Message {message.Id} completed. " +
                $"Waiting {delay.TotalSeconds:F2} seconds before next message. " +
                $"BatchProgress={messagesInCurrentBatch}/{currentBatchSize}");

            try
            {
                await Task.Delay(
                    delay,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        await _logger.LogInfoAsync(
            "[PROCESS] Message worker stopped.");
    }

    private async Task HandleBatchCompletedAsync(
        int batchSize,
        int targetBatchSize,
        CancellationToken cancellationToken)
    {
        await _logger.LogInfoAsync(
            $"[BATCH] Batch completed. " +
            $"Size={batchSize}/{targetBatchSize}, " +
            $"QueueSize={_queue.Count}");

        TimeSpan batchDelay = GetBatchDelay();

        await _logger.LogInfoAsync(
            $"[BATCH-DELAY] Waiting {batchDelay.TotalSeconds:F2} seconds " +
            $"before next batch.");

        await Task.Delay(batchDelay, cancellationToken);
    }

    private async Task<bool> SendMessageAsync(
        OutboxMessage message)
    {
        if (message?.Recipient is null)
        {
            await _logger.LogInfoAsync(
                $"[SEND] Message {message?.Id} ignored because recipient is missing.");

            return false;
        }

        if (string.IsNullOrWhiteSpace(message.Recipient.PhoneNumber))
        {
            await _logger.LogInfoAsync(
                $"[SEND] Message {message.Id} failed because recipient phone number is empty.");

            return false;
        }

        StringBuilder messageTextBuilder = new();

        messageTextBuilder.AppendLine(message.Text);

        if (message.Notes is not null && message.Notes.Any())
        {
            messageTextBuilder.AppendLine("*„·«ÕŸ« :*");
            messageTextBuilder.AppendLine("----------------------");

            foreach (string note in message.Notes)
            {
                messageTextBuilder.AppendLine($"= : {note}");
            }

            messageTextBuilder.AppendLine("*„⁄  ÕÌ«  „ Ã— «·„”«Ãœ*");
        }

        List<string> attachments = message.Attachments
            .Select(m =>
                Path.Combine(
                    _directoriesService.AttachmentsDirectory,
                    m.StoredFileName))
            .ToList();

        await _logger.LogInfoAsync(
            $"[SEND] Message {message.Id} prepared. " +
            $"Type={message.RecipientType}, " +
            $"Attachments={attachments.Count}, " +
            $"TextLength={messageTextBuilder.Length}");

        MessageToSend messageToSend =
            new(
                messageTextBuilder.ToString(),
                attachments);

        return message.RecipientType switch
        {
            RecipientType.Supplier or RecipientType.Deliveryman
                => await _messageSender.SendToGroupAsync(
                    message.Recipient.PhoneNumber,
                    messageToSend),

            _ => await _messageSender.SendToPhoneAsync(
                message.Recipient.PhoneNumber,
                messageToSend)
        };
    }

    private async Task UpdateMessageStatusAsync(
        int messageId,
        OutboxMessageStatus status,
        bool sent)
    {
        using AppDbContext db = _dbFactory.CreateDbContext();

        OutboxMessage entity =
            await db.OutboxMessages.FindAsync(messageId);

        if (entity is null)
        {
            await _logger.LogInfoAsync(
                $"[STATUS] Message {messageId} not found while updating status to {status}.");

            return;
        }

        OutboxMessageStatus previousStatus = entity.Status;
        DateTime attemptAt = DateTime.Now;

        entity.Status = status;

        entity.LastAttemptAt = attemptAt;
        entity.SentAt = sent ? attemptAt : null;

        await db.SaveChangesAsync();
        WeakReferenceMessenger.Default.Send(new OrderHub.Application.Messages.OutboxMessages.MessageStatusChangedMessage(
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
            await _logger.LogInfoAsync(
                "[SERVICE] Stopping message service.");

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

            await _logger.LogInfoAsync(
                "[SERVICE] Message service stopped.");
        }
        finally
        {
            _stateLock.Release();
        }
    }

    private int GetBatchSize()
    {
        return Random.Shared.Next(
            _options.MinBatchSize,
            _options.MaxBatchSize + 1);
    }

    private TimeSpan GetMessageDelay()
    {
        return TimeSpan.FromSeconds(
            Random.Shared.Next(
                _options.MinMessageDelaySeconds,
                _options.MaxMessageDelaySeconds + 1));
    }

    private TimeSpan GetBatchDelay()
    {
        return TimeSpan.FromSeconds(
            Random.Shared.Next(
                _options.MinBatchDelaySeconds,
                _options.MaxBatchDelaySeconds + 1));
    }
}

public sealed class MessageSendingOptions
{
    public int MinBatchSize { get; init; } = 8;
    public int MaxBatchSize { get; init; } = 12;

    public int MinMessageDelaySeconds { get; init; } = 10;
    public int MaxMessageDelaySeconds { get; init; } = 20;

    public int MinBatchDelaySeconds { get; init; } = 60;
    public int MaxBatchDelaySeconds { get; init; } = 120;
}