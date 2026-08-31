using MediatR;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Application.Interfaces.Stores;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Messaging;

public static class SendMessageCommand
{
    public record Command(Request Request) : IRequest<Result>;

    public record Request(
        int RecipientId,
        string RecipientName,
        string Destination,
        RecipientType RecipientType,
        MessageToSend Message);

    internal sealed class Handler : IRequestHandler<Command, Result>
    {
        private readonly IMessageService _messageService;
        private readonly IApplicationDirectoriesService _directoriesService;
        private readonly IOutboxMessageRepository _messageRepository;
        private readonly IUnitOfWork _unitOfWork;

        public Handler(
            IMessageService messageService,
            IApplicationDirectoriesService directoriesService,
            IOutboxMessageRepository messageRepository,
            IUnitOfWork unitOfWork)
        {
            _messageService = messageService;
            _directoriesService = directoriesService;
            _messageRepository = messageRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var recipient = CreateReipient(
                request.Request.RecipientId,
                request.Request.RecipientName,
                request.Request.Destination,
                request.Request.RecipientType);

            List<OutboxMessageAttachment> attachments = request.Request
                .Message
                .Attachments
                .Select(CreateAttachment)
                .ToList();

            OutboxMessage outboxMessage = new OutboxMessage()
            {
                Text = request.Request.Message.Message,
                RecipientType = request.Request.RecipientType,
                Attachments = attachments,
                Status = Domain.Enums.OutboxMessageStatus.Sending,
                Recipient = recipient,
                LastAttemptAt = DateTime.UtcNow,
            };


            await _messageRepository.Create(outboxMessage);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _messageService.QueueMessage(outboxMessage);
                return Result.Success();
            }
            catch (System.Exception)
            {
                return Result.Failure("فشل ارسال الرسالة");
            }
        }

        private OutboxMessageRecipient CreateReipient(int id, string name, string destination, RecipientType recipientType)
        {
            return recipientType switch
            {
                Domain.Enums.RecipientType.Client => new ClientRecipient
                {
                    ClientId = id,
                    Name = name,
                    PhoneNumber = destination,
                },

                Domain.Enums.RecipientType.Deliveryman => new DeliverymanRecipient
                {
                    DeliveryManId = id,
                    Name = name,
                    PhoneNumber = destination
                },

                Domain.Enums.RecipientType.Supplier => new SupplierRecipient
                {
                    SupplierId = id,
                    Name = name,
                    PhoneNumber = destination
                },

                Domain.Enums.RecipientType.ShippingCarrier => new ShippingCarrierRecipient
                {
                    ShippingCarrierId = id,
                    Name = name,
                    PhoneNumber = destination
                },
                _ => null
            };
        }

        private OutboxMessageAttachment CreateAttachment(string file)
        {
            string originalFileName = Path.GetFileName(file);
            string extension = Path.GetExtension(file);
            string storedFileName = $"{Guid.NewGuid()}{extension}";

            string destinationPath = Path.Combine(_directoriesService.AttachmentsDirectory, storedFileName);
            File.Copy(file, destinationPath, true);
            var attachment = new OutboxMessageAttachment(originalFileName, storedFileName, extension, new FileInfo(destinationPath).Length);
            return attachment;
        }
    }
}
