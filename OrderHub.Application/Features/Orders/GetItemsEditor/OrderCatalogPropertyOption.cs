using MediatR;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.GetItemsEditor;

public record OrderCatalogPropertyOption(
        int Id,
        string Name);
