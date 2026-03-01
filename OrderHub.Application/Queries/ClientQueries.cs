using MediatR;
using OrderHub.Application.Common;
using System.Collections.Generic;
using static OrderHub.Application.DTOs.ClientDtos;

namespace OrderHub.Application.Queries;

public class ClientQueries
{
    public record GetAllClientsQuery : IRequest<IEnumerable<ClientListDto>>;
    public record GetAllClientsPagedQuery(int PageNumber = 1, int PageSize = 20) : IRequest<PagedResult<ClientListDto>>;
    public record GetClientEditQuery(int Id) : IRequest<ClientEditDto>;
}
