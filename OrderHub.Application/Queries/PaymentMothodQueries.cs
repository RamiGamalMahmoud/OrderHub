using MediatR;
using System.Collections.Generic;
using static OrderHub.Application.DTOs.PaymentMothodsDtos;

namespace OrderHub.Application.Queries;

public static class PaymentMothodQueries
{
    public record GetPaymentMethodListQuery() : IRequest<IEnumerable<PaymentMethodListDto>>;

}
