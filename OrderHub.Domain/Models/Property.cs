using OrderHub.Domain.Enums;
using OrderHub.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.Domain.Models;

public class Property : ModelBase
{
    private readonly List<PropertyOption> _options = [];

    public string Name { get; private set; } = null!;

    public PropertyType PropertyType { get; private set; }

    public IReadOnlyCollection<PropertyOption> Options => _options;

    private Property()
    {
    }

    public static Property Create(string name, PropertyType propertyType, IEnumerable<string> options = null)
    {
        ValidateName(name);

        Property property = new()
        {
            Name = name.Trim(),
            PropertyType = propertyType
        };

        if (propertyType == PropertyType.List)
        {
            if (options is null || !options.Any())
                throw new DomainException("List property must contain at least one option.");

            foreach (string option in options)
                property.AddOption(option);
        }
        else
        {
            if (options is not null && options.Any())
                throw new DomainException("Only list properties can contain options.");
        }

        return property;
    }

    public void Rename(string name)
    {
        ValidateName(name);

        Name = name.Trim();
    }

    public void ChangeType(PropertyType propertyType)
    {
        if (PropertyType == propertyType)
            return;

        if (propertyType != PropertyType.List && _options.Any())
            throw new DomainException("Cannot change the property type while it contains options.");

        PropertyType = propertyType;
    }

    public void AddOption(string value)
    {
        EnsureListProperty();

        ValidateOptionValue(value);

        EnsureOptionDoesNotExist(value);

        _options.Add(PropertyOption.Create(value.Trim()));
    }

    public void RenameOption(int optionId, string value)
    {
        EnsureListProperty();

        ValidateOptionValue(value);

        var option = GetOption(optionId);

        if (option is null)
            throw new DomainException("Property option was not found.");

        EnsureOptionDoesNotExist(value, optionId);

        option.Rename(value.Trim());
    }

    public void RemoveOption(int optionId)
    {
        EnsureListProperty();

        var option = GetOption(optionId);

        if (option is null)
            throw new DomainException("Property option was not found.");

        _options.Remove(option);
    }

    public PropertyOption GetOption(int optionId)
    {
        return _options.FirstOrDefault(x => x.Id == optionId);
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Property name is required.");
    }

    private static void ValidateOptionValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Property option value is required.");
    }

    private void EnsureListProperty()
    {
        if (PropertyType != PropertyType.List)
            throw new DomainException("Only list properties can contain options.");
    }

    private void EnsureOptionDoesNotExist(string value, int? ignoredOptionId = null)
    {
        var exists = _options.Any(x =>
            x.Id != ignoredOptionId &&
            string.Equals(x.Value, value.Trim(), StringComparison.OrdinalIgnoreCase));

        if (exists)
            throw new DomainException($"The option '{value}' already exists.");
    }
}