using MediatR;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Stores;
using OrderHub.Domain.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Messaging;

public static class DeleteOutboxMessage
{
    public record Command(int MessageId) : IRequest<Result>;

    internal sealed class Handler : IRequestHandler<Command, Result>
    {
        private readonly IOutboxMessageRepository _messageRepository;
        private readonly IUnitOfWork _unitOfWork;

        public Handler(IOutboxMessageRepository messageRepository, IUnitOfWork unitOfWork)
        {
            _messageRepository = messageRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var message = await _messageRepository.GetForResend(request.MessageId, cancellationToken);

            if (message is null)
            {
                return Result.Failure("الرسالة غير موجودة.");
            }

            await _messageRepository.DeleteAsync(message, cancellationToken);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }
            catch(Exception)
            {
                return Result.Failure("لا يمكن حذف هذه الرسالة.");
            }
        }
    }
}
