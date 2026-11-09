#nullable enable

namespace OrderHub.Application.Features.Administration.Roles.GetRoles;

public sealed record RoleListItem(
    int Id,
    string Name,
    bool IsSealed,
    string? RoleKey,
    int ReadCount,
    int WriteCount,
    int ApproveCount)
{
    public int PermissionCount => ReadCount + WriteCount + ApproveCount;
}
