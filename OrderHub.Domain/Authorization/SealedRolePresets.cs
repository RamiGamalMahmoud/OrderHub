#nullable enable

using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.Domain.Authorization;

public sealed record RolePermissionPreset(
    PermissionFeature Feature,
    PermissionLevel Level);

public sealed record SealedRolePreset(
    string RoleKey,
    string Name,
    IReadOnlyList<RolePermissionPreset> Permissions);

public static class SealedRolePresets
{
    public const string SalesKey = "sales";
    public const string PurchasingKey = "purchasing";
    public const string DataEntryKey = "data-entry";
    public const string ManagerKey = "manager";

    public static IReadOnlyList<SealedRolePreset> All { get; } =
    [
        new(
            SalesKey,
            "مبيعات",
            [
                new(PermissionFeature.Orders, PermissionLevel.Write),
                new(PermissionFeature.Documents, PermissionLevel.Read),
                new(PermissionFeature.Products, PermissionLevel.Read),
                new(PermissionFeature.Categories, PermissionLevel.Read),
                new(PermissionFeature.Clients, PermissionLevel.Write),
                new(PermissionFeature.Deliverymen, PermissionLevel.Read),
                new(PermissionFeature.ShippingCarriers, PermissionLevel.Read),
                new(PermissionFeature.Cities, PermissionLevel.Read),
                new(PermissionFeature.PaymentMethods, PermissionLevel.Read)
            ]),

        new(
            PurchasingKey,
            "مشتريات",
            [
                new(PermissionFeature.Orders, PermissionLevel.Read),
                new(PermissionFeature.Documents, PermissionLevel.Read),
                new(PermissionFeature.Products, PermissionLevel.Read),
                new(PermissionFeature.Suppliers, PermissionLevel.Write),
                new(PermissionFeature.Cities, PermissionLevel.Read),
                new(PermissionFeature.PaymentMethods, PermissionLevel.Read)
            ]),

        new(
            DataEntryKey,
            "إدخال البيانات",
            [
                new(PermissionFeature.Products, PermissionLevel.Write),
                new(PermissionFeature.Categories, PermissionLevel.Write),
                new(PermissionFeature.Properties, PermissionLevel.Write),
                new(PermissionFeature.Cities, PermissionLevel.Write),
                new(PermissionFeature.PaymentMethods, PermissionLevel.Read)
            ]),

        new(
            ManagerKey,
            "مدير",
            [
                new(PermissionFeature.Orders, PermissionLevel.Approve),
                new(PermissionFeature.Documents, PermissionLevel.Approve),
                new(PermissionFeature.Products, PermissionLevel.Approve),
                new(PermissionFeature.Categories, PermissionLevel.Approve),
                new(PermissionFeature.Properties, PermissionLevel.Approve),
                new(PermissionFeature.Clients, PermissionLevel.Approve),
                new(PermissionFeature.Suppliers, PermissionLevel.Approve),
                new(PermissionFeature.Deliverymen, PermissionLevel.Approve),
                new(PermissionFeature.ShippingCarriers, PermissionLevel.Approve),
                new(PermissionFeature.Cities, PermissionLevel.Approve),
                new(PermissionFeature.PaymentMethods, PermissionLevel.Approve)
            ])
    ];

    public static bool IsReservedName(string? name)
    {
        string candidate = name?.Trim() ?? string.Empty;

        return All.Any(preset =>
            string.Equals(
                preset.Name,
                candidate,
                StringComparison.OrdinalIgnoreCase));
    }

    public static SealedRolePreset? FindByKey(string? key) =>
        All.FirstOrDefault(preset =>
            string.Equals(
                preset.RoleKey,
                key,
                StringComparison.Ordinal));
}
