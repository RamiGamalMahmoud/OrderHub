using OrderHub.Domain.Enums;
using System;

namespace OrderHub.Application.Authorization;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RequirePermissionAttribute(PermissionFeature feature, PermissionLevel requiredLevel) : Attribute
{
    public PermissionFeature Feature { get; } = feature;
    public PermissionLevel RequiredLevel { get; } = requiredLevel;
}
