using System.Collections.Generic;

namespace OrderHub.Application.Interfaces.Services;

public record MessageToSend(string Message, IReadOnlyCollection<string> Attachments)
{
    public bool HasAttachments => Attachments is not null && Attachments.Count > 0;
}
