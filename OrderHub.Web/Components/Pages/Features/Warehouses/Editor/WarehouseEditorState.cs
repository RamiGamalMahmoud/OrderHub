using OrderHub.Application.Features.Warehouses.Contracts;
using OrderHub.Application.Features.Warehouses.GetKeeperCandidates;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.Web.Components.Pages.Features.Warehouses.Editor;

public sealed class WarehouseEditorState
{
    public string Name { get; set; } = string.Empty;

    public int? KeeperUserId { get; set; }

    public bool IsActive { get; set; } = true;

    public List<WarehouseKeeperCandidate> Candidates { get; } = [];

    public WarehouseKeeperCandidate? SelectedKeeper =>
        KeeperUserId is int keeperId
            ? Candidates.FirstOrDefault(candidate => candidate.Id == keeperId)
            : null;

    public static WarehouseEditorState FromWarehouse(WarehouseEdit warehouse) => new()
    {
        Name = warehouse.Name,
        KeeperUserId = warehouse.KeeperUserId,
        IsActive = warehouse.IsActive
    };

    public static string? ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2 || name.Trim().Length > 100)
        {
            return "اسم المخزن يجب أن يتكون من 2 إلى 100 حرف.";
        }

        return null;
    }
}
