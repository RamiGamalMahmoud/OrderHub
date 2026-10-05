using MediatR;
using OrderHub.Domain.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.GetItemEditor;

public record OrderItemEditorData(
        int ProductId,
        decimal Price,
        string Name,
        string CategoryName,
        IEnumerable<OrderItemEditorSupplier> Suppliers,
        IEnumerable<OrderItemEditorProperty> Properties);
