using MediatR;
using System.Collections.Generic;
using static OrderHub.Application.DTOs.ClientDtos;

namespace OrderHub.Application.Queries;

public class ClientQueries
{
    public record GetAllClientsQuery : IRequest<IEnumerable<ClientListDto>>;
    public record GetClientEditQuery(int Id) : IRequest<ClientEditDto>;
}
