using MediatR;
using OrderHub.Domain.Enums;
using System.Collections.Generic;
using System.Threading;
using static OrderHub.Application.DTOs.WhatsappGroupDtos;

namespace OrderHub.Application.Queries;

public static class WhatsappGroupQueries
{
    public record GetAllWhatsappGroupListsQuery : IRequest<IEnumerable<WhatsappGroupListDto>>;
    public record GetAllWhatsappGroupInfosQuery(WhatsappGroupType GroupType) : IRequest<IEnumerable<WhatsappGroupInfoDto>>;
    public record GetAllWhatsappGroupForEditQuery(int Id) : IRequest<WhatsappGroupEditDto>;
    public record IsWhatsappGroupExistsQuery(string GroupName, CancellationToken CancellationToken) : IRequest<bool>;
}
