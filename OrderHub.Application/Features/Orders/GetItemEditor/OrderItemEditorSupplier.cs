using MediatR;
using OrderHub.Domain.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.GetItemEditor;

public record OrderItemEditorSupplier(int Id, string Name);
