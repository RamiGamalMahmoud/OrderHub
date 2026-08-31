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

public static class ResendOutboxMessageCommand
{
    public record Command(Message Message) : IRequest<Result>;

    internal sealed class Handler : IRequestHandler<Command, Result>
    {
        private readonly IOutboxMessageRepository _messageRepository;
        private readonly IMessageService _messageService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;

        public Handler(
            IOutboxMessageRepository messageRepository,
            IMessageService messageService,
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService)
        {
            _messageRepository = messageRepository;
            _messageService = messageService;
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            // 1. Get the message from database
            var message = await _messageRepository.GetForResend(
                request.Message.Id,
                cancellationToken);

            if (message is null)
                return Result.Failure("الرسالة غير موجودة.");

            // 2. Synchronize attachments
            //    - Add new attachments to the aggregate
            //    - Remove deleted attachments from the aggregate
            //    - Keep existing attachments
            //    - Return physical file operations
            var attachmentSyncResult = SynchronizeAttachments(message, request.Message.Attachments);

            // 3. Update the message entity
            message.Text = request.Message.Text;
            message.Notes = request.Message.Notes.ToList();
            message.Status = OutboxMessageStatus.Sending;
            message.RetryCount = (message.RetryCount ?? 0) + 1;
            message.LastAttemptAt = DateTime.UtcNow;
            message.SentAt = null;

            // 4. Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 5. Store new physical files.
            foreach (var attachment in attachmentSyncResult.FilesToStore)
            {
                _fileStorageService.SaveAttachment(attachment.SourcePath, attachment.StoredFileName);
            }

            foreach(var fileToRemove in attachmentSyncResult.FilesToRemove)
            {
                _fileStorageService.RemoveAttachmentFile(fileToRemove);
            }

            // 6. Queue the message
            _messageService.QueueMessage(message);
            return Result.Success();
        }

        private static AttachmentSynchronizationResult SynchronizeAttachments(OutboxMessage message, IEnumerable<AttachmentFile> attachments)
        {
            var newFiles = new List<FileToStore>();
            var filesToRemove = new List<string>();

            var requestedAttachments = attachments.ToDictionary(
                x => x.OriginalFileName,
                StringComparer.OrdinalIgnoreCase);

            var existingAttachments = message.Attachments.ToDictionary(
                x => x.OriginalFileName,
                StringComparer.OrdinalIgnoreCase);

            // Add new
            foreach (var attachmentFile in requestedAttachments.Values
                .Where(x => !existingAttachments.ContainsKey(x.OriginalFileName)))
            {
                var fileInfo = new FileInfo(attachmentFile.FilePath);

                var storedFileName =
                    $"{Guid.NewGuid()}{fileInfo.Extension}";

                message.Attachments.Add(
                    new OutboxMessageAttachment(
                        attachmentFile.OriginalFileName,
                        storedFileName,
                        fileInfo.Extension,
                        fileInfo.Length));

                newFiles.Add(
                    new FileToStore(
                        attachmentFile.FilePath,
                        storedFileName));
            }

            // Remove deleted
            foreach (var attachment in existingAttachments.Values
                .Where(x => !requestedAttachments.ContainsKey(x.OriginalFileName)))
            {
                message.Attachments.Remove(attachment);
                filesToRemove.Add(attachment.StoredFileName);
            }

            return new AttachmentSynchronizationResult(newFiles, filesToRemove);
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

    public record AttachmentSynchronizationResult(
        IReadOnlyCollection<FileToStore> FilesToStore,
        IReadOnlyCollection<string> FilesToRemove);
}
