using MediatR;
using System.Collections.Generic;
using static OrderHub.Application.DTOs.OrderDtos;

namespace OrderHub.Application.Queries;

public static class OrderQueries
{
    public record GetOrdersQuery : IRequest<IEnumerable<OrderListDto>>;
    public record GetClientOrdersQuery(string ClientName) : IRequest<IEnumerable<OrderListDto>>;
}
