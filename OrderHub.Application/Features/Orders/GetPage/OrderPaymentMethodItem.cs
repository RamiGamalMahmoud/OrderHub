using MediatR;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Lookups;
using OrderHub.Domain.Enums;
using System;

namespace OrderHub.Application.Features.Orders.GetPage;

public record OrderPaymentMethodItem(int Id, string DisplayName, string Description, bool IsActive);
