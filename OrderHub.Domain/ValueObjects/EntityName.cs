using System;

namespace OrderHub.Domain.ValueObjects;

public class EntityName
{
    public string Value { get; }
    private EntityName()
    {
        
    }
    public EntityName(string value, int minLength = 2, int maxLength = 100)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Name cannot be empty");

        if (value.Length < minLength)
            throw new ArgumentException($"Name cannot be less than {minLength} characters");

        if (value.Length > maxLength)
            throw new ArgumentException($"Name cannot exceed {maxLength} characters");
        Value = value;
    }
    public override string ToString() => Value;
}
