//using MediatR;
//using OrderHub.Application.Interfaces;
//using OrderHub.Application.Interfaces.Services;
//using OrderHub.Application.Interfaces.Stores;
//using OrderHub.Domain.Common;
//using OrderHub.Domain.Enums;
//using OrderHub.Domain.Models;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;

//namespace OrderHub.Application.Features.Messaging;

//public static class SendMessageCommand
//{
//    public record Command(Request Request) : IRequest<Result>;

//    public record Request(
//        int RecipientId,
//        string RecipientName,
//        string Destination,
//        RecipientType RecipientType,
//        MessageToSend Message);

//    internal sealed class Handler : IRequestHandler<Command, Result>
//    {
//        private readonly IMessageService _messageService;
//        private readonly ILocalFileAccessService _fileStorageService;
//        private readonly IOutboxMessageRepository _messageRepository;
//        private readonly IUnitOfWork _unitOfWork;

//        public Handler(
//            IMessageService messageService,
//            IOutboxMessageRepository messageRepository,
//            IUnitOfWork unitOfWork,
//            ILocalFileAccessService fileStorageService)
//        {
//            _messageService = messageService;
//            _messageRepository = messageRepository;
//            _unitOfWork = unitOfWork;
//            _fileStorageService = fileStorageService;
//        }

//        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
//        {
//            var recipient = CreateReipient(
//                request.Request.RecipientId,
//                request.Request.RecipientName,
//                request.Request.Destination,
//                request.Request.RecipientType);

//            IEnumerable<Task<OutboxMessageAttachment>> tasks = request.Request.Message.Attachments.Select(CreateAttachment).ToList();

//            var attachments = (await Task.WhenAll(tasks)).ToList();

//            OutboxMessage outboxMessage = new OutboxMessage()
//            {
//                Text = request.Request.Message.Message,
//                RecipientType = request.Request.RecipientType,
//                Attachments = attachments,
//                Status = Domain.Enums.OutboxMessageStatus.Sending,
//                Recipient = recipient,
//                LastAttemptAt = DateTime.UtcNow,
//            };


//            await _messageRepository.Create(outboxMessage);

//            try
//            {
//                await _unitOfWork.SaveChangesAsync(cancellationToken);
//                _messageService.QueueMessage(outboxMessage);
//                return Result.Success();
//            }
//            catch (System.Exception)
//            {
//                return Result.Failure("فشل ارسال الرسالة");
//            }
//        }

//        private OutboxMessageRecipient CreateReipient(int id, string name, string destination, RecipientType recipientType)
//        {
//            return recipientType switch
//            {
//                Domain.Enums.RecipientType.Client => new ClientRecipient
//                {
//                    ClientId = id,
//                    Name = name,
//                    PhoneNumber = destination,
//                },

//                Domain.Enums.RecipientType.Deliveryman => new DeliverymanRecipient
//                {
//                    DeliveryManId = id,
//                    Name = name,
//                    PhoneNumber = destination
//                },

//                Domain.Enums.RecipientType.Supplier => new SupplierRecipient
//                {
//                    SupplierId = id,
//                    Name = name,
//                    PhoneNumber = destination
//                },

//                Domain.Enums.RecipientType.ShippingCarrier => new ShippingCarrierRecipient
//                {
//                    ShippingCarrierId = id,
//                    Name = name,
//                    PhoneNumber = destination
//                },
//                _ => null
//            };
//        }

//        private async Task<OutboxMessageAttachment> CreateAttachment(string file)
//        {
//            //string extension = Path.GetExtension(file);
//            //string storedFileName = $"{Guid.NewGuid()}{extension}";

//            //    await _fileStorageService.SaveAttachmentAsync(
//            //        file,
//            //        storedFileName);

//            //return new OutboxMessageAttachment(
//            //    storedFile.OriginalFileName,
//            //    storedFile.StoredFileName,
//            //    Path.GetExtension(storedFile.OriginalFileName),
//            //    (long)storedFile.Size);
//            return null;
//        }
//    }
//}
