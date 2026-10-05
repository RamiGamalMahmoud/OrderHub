using MediatR;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.GetItemsEditor;

public record OrderCatalogSupplier(
        int Id,
        string Name);
