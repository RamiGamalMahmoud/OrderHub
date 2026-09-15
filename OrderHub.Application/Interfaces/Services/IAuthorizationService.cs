using OrderHub.Domain.Enums;
using System;

namespace OrderHub.Application.Interfaces.Services;

public interface IAuthorizationService
{
    event EventHandler AuthorizationChanged;
    bool CanRead(PermissionFeature feature);
    bool CanWrite(PermissionFeature feature);
}