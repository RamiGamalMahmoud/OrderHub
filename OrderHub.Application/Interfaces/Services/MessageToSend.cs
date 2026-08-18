using System.Collections.Generic;

namespace OrderHub.Application.Interfaces.Services;

public record MessageToSend(string Message, IReadOnlyCollection<MessageAttachment> Attachments)
{
    public bool HasAttachments => Attachments is not null && Attachments.Count > 0;
}

public record MessageAttachment(string Path);
