using MediatR;
using System.Collections.Generic;
using static OrderHub.Application.DTOs.DeliverymanDtos;

namespace OrderHub.Application.Queries;

public static class DeliverymanQueries
{
    public record GetAllDeliverymenListQuery : IRequest<IEnumerable<DeliverymanListDto>>;
    public record GetDeliverymanForEditQuery(int Id) : IRequest<DeliverymanFormDto>;
    public record GetDeliverymenByNameQuery(string SearchTerm = null, int Take = 20) : IRequest<IEnumerable<DeliverymanListDto>>;
    public record GetDeliverymanByIdQuery(int Id) : IRequest<DeliverymanListDto>;
}
