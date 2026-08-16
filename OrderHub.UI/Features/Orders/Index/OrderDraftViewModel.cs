using System;

namespace OrderHub.UI.Features.Orders.Index;

internal sealed record OrderDraftViewModel(
    Guid Id,
    string ClientName,
    int ItemsCount,
    DateTime UpdatedAt);