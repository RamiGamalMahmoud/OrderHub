using OrderHub.Application.Context;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Interfaces.Services;

public interface IAuthorizationService
{
    bool CanRead(PermissionFeature feature, UserSession user);
    bool CanWrite(PermissionFeature feature, UserSession user);
}