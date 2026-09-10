using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Administration.Admin.CreateAdmin;
public sealed record CreateAdminCommand(
    string UserName,
    string DisplayName,
    string Password) : IRequest<Result>;