using OrderHub.Domain.Enums;

namespace OrderHub.Web.Components.Pages.Features.Administration.Roles.Editor;

public sealed class RolePermissionEditorState
{
    public PermissionFeature Feature { get; init; }

    public PermissionLevel Level { get; set; } = PermissionLevel.None;
}