using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Context;
public sealed record UserSession(
    int Id,
    string UserName,
    string DisplayName,
    bool IsAdmin,
    IReadOnlyCollection<UserPermissionSession> Permissions);

public sealed record UserPermissionSession(
    PermissionFeature Feature,
    bool CanRead,
    bool CanWrite);