using MediatR;
using OrderHub.Application.Common;
using System.Collections.Generic;
using OrderHub.Application.Features.Setup.Clients.Contracts;

namespace OrderHub.Application.Features.Setup.Clients;

public record GetAllClientsPagedQuery(int PageNumber = 1, int PageSize = 20) : IRequest<PagedResult<ClientListItem>>;
