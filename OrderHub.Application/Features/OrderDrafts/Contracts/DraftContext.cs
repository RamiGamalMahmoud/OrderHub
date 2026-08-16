using System;

namespace OrderHub.Application.Features.OrderDrafts.Contracts;

public sealed class DraftContext
{
    public Guid Id { get; }

    public DraftContext(Guid id)
    {
        Id = id;
    }
}