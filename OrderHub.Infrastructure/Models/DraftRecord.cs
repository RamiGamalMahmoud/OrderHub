using System;

namespace OrderHub.Infrastructure.Models;
internal sealed class DraftRecord
{
    public Guid Id { get; set; }

    public int UserId { get; set; }

    public string Data { get; set; }

    public DateTime LastModified { get; set; }
}
