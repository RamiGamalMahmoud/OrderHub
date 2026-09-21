using MediatR;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Stores;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Messaging;
public record MarkMessageSentCommand(int MessageId) : IRequest<Result<MarkMessageSentResponse>>;

internal class MarkMessageSentHandler : IRequestHandler<MarkMessageSentCommand, Result<MarkMessageSentResponse>>
{
    private readonly IOutboxMessageRepository _messageRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkMessageSentHandler(
        IOutboxMessageRepository messageRepository,
        IUnitOfWork unitOfWork)
    {
        _messageRepository = messageRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MarkMessageSentResponse>> Handle(MarkMessageSentCommand request, CancellationToken cancellationToken)
    {
        OutboxMessage message = await _messageRepository.GetById(request.MessageId);
        message.Status = OutboxMessageStatus.Sent;
        message.LastAttemptAt = DateTime.UtcNow;

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<MarkMessageSentResponse>.Success(new MarkMessageSentResponse(
                message.Status,
                message.LastAttemptAt));
        }
        catch (System.Exception)
        {
            return  Result<MarkMessageSentResponse>.Failure("");
        }
    }
}

public record MarkMessageSentResponse(OutboxMessageStatus Status, DateTime? StatusChangedAt);
