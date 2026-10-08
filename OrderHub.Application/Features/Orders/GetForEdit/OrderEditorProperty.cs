using MediatR;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.GetForEdit;

public record OrderEditorProperty(
        int PropertyId,
        string Name,
        bool IsRequired,
        PropertyType PropertyType,
        IEnumerable<OrderEditorOption> Options,
        string SelectedValue = null);
