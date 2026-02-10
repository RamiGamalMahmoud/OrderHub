using System;

namespace OrderHub.Domain.Models;

public abstract class ModelBase
{
    public int Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime? ModifiedAt { get; protected set; }

    protected ModelBase()
    {
        UpdateCreationDate();
        ModifiedAt = null;
    }

    public void UpdateCreationDate(DateTime? createdAt = null)
    {
        CreatedAt = createdAt ?? DateTime.Now;
        UpdateModificationDate(CreatedAt);
    }

    public void UpdateModificationDate(DateTime modifiedAt) => ModifiedAt = modifiedAt;

    public override bool Equals(object obj)
    {
        if (obj is null || GetType() != obj.GetType()) return false;
        return Id == (obj as ModelBase).Id;
    }
    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(ModelBase left, ModelBase right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ModelBase left, ModelBase right) => !(left == right);
}
