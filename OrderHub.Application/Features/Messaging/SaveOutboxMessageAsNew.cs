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
public static class SaveOutboxMessageAsNew
{
    public record Command(Message Message) : IRequest<Result>;

    internal sealed class Handler : IRequestHandler<Command, Result>
    {
        private readonly IOutboxMessageRepository _messageRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILocalFileAccessService _fileStorageService;

        public Handler(
            IOutboxMessageRepository messageRepository,
            IUnitOfWork unitOfWork,
            ILocalFileAccessService fileStorageService)
        {
            _messageRepository = messageRepository;
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
        }

        public async Task<Result> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            // الرسالة الأصلية نحتاجها للحصول على بيانات
            // لا يسمح للمستخدم بتعديلها مثل Recipient / Order
            var sourceMessage = await _messageRepository.GetForResend(
                request.Message.Id,
                cancellationToken);

            if (sourceMessage is null)
                return Result.Failure("الرسالة الأصلية غير موجودة.");

            var newMessage = new OutboxMessage
            {
                Text = request.Message.Text,

                OrderId = sourceMessage.OrderId,

                RecipientType = sourceMessage.RecipientType,
                RecipientId = sourceMessage.RecipientId,

                Notes = request.Message.Notes.ToList(),

                Status = OutboxMessageStatus.Pending,

                RetryCount = 0,
                MaxRetries = sourceMessage.MaxRetries,

                LastAttemptAt = DateTime.UtcNow,
                SentAt = DateTime.UtcNow
            };

            var filesToStore = AddAttachments(
                newMessage,
                request.Message.Attachments);

            await _messageRepository.Create(newMessage);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var file in filesToStore)
            {
                //await _fileStorageService.SaveAttachmentAsync(
                //    file.SourcePath,
                //    file.StoredFileName);
            }

            return Result.Success();
        }

        private static IReadOnlyCollection<FileToStore> AddAttachments(
            OutboxMessage message,
            IEnumerable<AttachmentFile> attachments)
        {
            var filesToStore = new List<FileToStore>();

            foreach (var attachment in attachments)
            {
                var fileInfo = new FileInfo(attachment.FilePath);

                var storedFileName =
                    $"{Guid.NewGuid()}{fileInfo.Extension}";

                message.Attachments.Add(
                    new OutboxMessageAttachment(
                        attachment.OriginalFileName,
                        storedFileName,
                        fileInfo.Extension,
                        fileInfo.Length));

                filesToStore.Add(
                    new FileToStore(
                        attachment.FilePath,
                        storedFileName));
            }

            return filesToStore;
        }
    }

    public record Message(
        int Id,
        string Text,
        IReadOnlyCollection<AttachmentFile> Attachments,
        IReadOnlyList<string> Notes);

    public record AttachmentFile(
        string FilePath,
        string OriginalFileName);

    public record FileToStore(
        string SourcePath,
        string StoredFileName);
}
