using MediatR;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.GetForEdit;

public record OrderEditorItem(
        int ProductId,
        string ProductName,
        string CategoryName,
        decimal UnitPrice,
        int Quantity,
        int? SupplierId,
        string SupplierName,
        IEnumerable<OrderEditorSupplier> Suppliers,
        IEnumerable<OrderEditorProperty> Properties,
        decimal VatRate = 0);
