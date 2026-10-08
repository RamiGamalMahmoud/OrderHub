using OrderHub.Domain.Enums;

namespace OrderHub.Web.Components.Pages.Features.Administration.Roles.Editor;

public sealed class RoleEditorState
{
    public string Name { get; set; } = string.Empty;

    public List<RolePermissionEditorState> Permissions { get; } =
        Enum.GetValues<PermissionFeature>()
            .Select(feature => new RolePermissionEditorState
            {
                Feature = feature,
                Level = PermissionLevel.None
            })
            .ToList();

    public void ApplyPermissions(
        IEnumerable<(PermissionFeature Feature, PermissionLevel Level)> permissions)
    {
        ResetPermissions();

        foreach (var permission in permissions)
        {
            RolePermissionEditorState? target =
                Permissions.FirstOrDefault(
                    x => x.Feature == permission.Feature);

            if (target is not null)
                target.Level = permission.Level;
        }
    }

    public void ResetPermissions()
    {
        foreach (RolePermissionEditorState permission in Permissions)
            permission.Level = PermissionLevel.None;
    }
}