using MediatR;
using System.Collections.Generic;
using OrderHub.Application.Features.Setup.PaymentMethods.Contracts;

namespace OrderHub.Application.Features.Setup.PaymentMethods;

public record GetPaymentMethodListQuery() : IRequest<IEnumerable<PaymentMethodListItem>>;
