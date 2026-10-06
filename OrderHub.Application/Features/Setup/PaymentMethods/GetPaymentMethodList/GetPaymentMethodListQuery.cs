using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using System.Collections.Generic;
using OrderHub.Application.Features.Setup.PaymentMethods.Contracts;

namespace OrderHub.Application.Features.Setup.PaymentMethods;

[RequirePermission(PermissionFeature.PaymentMethods, PermissionLevel.Read)]
public record GetPaymentMethodListQuery() : IRequest<IEnumerable<PaymentMethodListItem>>;
