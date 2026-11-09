using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Domain.Enums;
using OrderHub.Application.Features.Administration.Roles.GetRole;
using OrderHub.Application.Features.Administration.Roles.GetRoles;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Features.Administration;

internal sealed class RoleReadService :
    IRoleDetailsService,
    IRoleListingService
{
    private readonly AppDbContextFactory _dbContextFactory;

    public RoleReadService(AppDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<RoleListItem>> GetAsync(
        CancellationToken cancellationToken = default)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();

        return await dbContext.Roles
            .AsNoTracking()
            .OrderByDescending(role => role.IsSealed)
            .ThenBy(role => role.Name)
            .Select(role => new RoleListItem(
                role.Id,
                role.Name,
                role.IsSealed,
                role.RoleKey,
                role.Permissions.Count(permission => permission.Level == PermissionLevel.Read),
                role.Permissions.Count(permission => permission.Level == PermissionLevel.Write),
                role.Permissions.Count(permission => permission.Level == PermissionLevel.Approve)))
            .ToListAsync(cancellationToken);
    }

    public async Task<GetRoleResponse> GetRoleDetailsAsync(
        int roleId,
        CancellationToken cancellationToken = default)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();

        return await dbContext.Roles
            .AsNoTracking()
            .Where(role => role.Id == roleId)
            .Select(role => new GetRoleResponse(
                role.Id,
                role.Name,
                role.IsSealed,
                role.RoleKey,
                role.Permissions
                    .Select(permission => new RolePermissionItem(
                        permission.Feature,
                        permission.Level))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }
}

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRoleReadService(
        this IServiceCollection services)
    {
        services.AddTransient<IRoleDetailsService, RoleReadService>();
        services.AddTransient<IRoleListingService, RoleReadService>();

        return services;
    }
}