using System;
using OrderHub.Domain.ValueObjects;

namespace OrderHub.Domain.Models;

public class Warehouse : ModelBase
{
    private Warehouse() { }

    public Warehouse(string name, int keeperUserId)
    {
        Rename(name);
        AssignKeeper(keeperUserId);
        IsActive = true;
    }

    public EntityName Name { get; private set; }

    public int KeeperUserId { get; private set; }

    public User Keeper { get; private set; }

    public bool IsActive { get; private set; }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = new EntityName(name.Trim());
    }

    public void AssignKeeper(int keeperUserId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(keeperUserId);

        KeeperUserId = keeperUserId;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public override string ToString() => Name.Value;
}
