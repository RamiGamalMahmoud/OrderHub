using System.Collections.Generic;

namespace OrderHub.Application.Context;
public sealed record UserSession(
    int Id,
    string UserName,
    string DisplayName,
    IReadOnlyCollection<string> Roles);