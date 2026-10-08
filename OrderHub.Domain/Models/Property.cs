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

    public string Description { get; private set; }

    public IReadOnlyCollection<PropertyOption> Options => _options.AsReadOnly();

    private Property()
    {
    }

    public static Property Create(
        string name,
        PropertyType propertyType,
        string description = null,
        IEnumerable<string> options = null)
    {
        ValidateName(name);
        ValidateType(propertyType);

        Property property = new()
        {
            Name = name.Trim(),
            PropertyType = propertyType,
            Description = description?.Trim()
        };

        if (propertyType == PropertyType.List)
        {
            if (options is null || !options.Any())
            {
                throw new DomainException("خاصية القائمة يجب أن تحتوي على خيار واحد على الأقل.");
            }

            foreach (string option in options)
            {
                property.AddOption(option);
            }
        }
        else
        {
            if (options is not null && options.Any())
            {
                throw new DomainException("الخيارات متاحة لخصائص القائمة فقط.");
            }
        }

        return property;
    }

    public void Rename(string name)
    {
        ValidateName(name);

        Name = name.Trim();
    }

    public void ChangeDescription(string description)
    {
        Description = description?.Trim();
    }

    public void ChangeType(PropertyType propertyType)
    {
        ValidateType(propertyType);

        if (PropertyType == propertyType)
        {
            return;
        }

        if (propertyType != PropertyType.List && _options.Any())
        {
            throw new DomainException("لا يمكن تغيير نوع الخاصية مع وجود خيارات.");
        }

        if (propertyType == PropertyType.List && !_options.Any())
        {
            throw new DomainException("خاصية القائمة يجب أن تحتوي على خيار واحد على الأقل.");
        }

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
        {
            throw new DomainException("الخيار غير موجود في هذه الخاصية.");
        }

        EnsureOptionDoesNotExist(value, optionId);

        option.Rename(value.Trim());
    }

    public void RemoveOption(int optionId)
    {
        EnsureListProperty();

        var option = GetOption(optionId);

        if (option is null)
        {
            throw new DomainException("الخيار غير موجود في هذه الخاصية.");
        }

        if (_options.Count == 1)
        {
            throw new DomainException("خاصية القائمة يجب أن تحتوي على خيار واحد على الأقل.");
        }

        _options.Remove(option);
    }

    public PropertyOption GetOption(int optionId)
    {
        return _options.FirstOrDefault(x => x.Id == optionId);
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
        {
            throw new DomainException("اسم الخاصية مطلوب ويجب ألا يتجاوز 100 حرف.");
        }
    }

    private static void ValidateOptionValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 100)
        {
            throw new DomainException("قيمة الخيار مطلوبة ويجب ألا تتجاوز 100 حرف.");
        }
    }

    private void EnsureListProperty()
    {
        if (PropertyType != PropertyType.List)
        {
            throw new DomainException("الخيارات متاحة لخصائص القائمة فقط.");
        }
    }

    private void EnsureOptionDoesNotExist(string value, int? ignoredOptionId = null)
    {
        var exists = _options.Any(x =>
            x.Id != ignoredOptionId &&
            string.Equals(x.Value, value.Trim(), StringComparison.OrdinalIgnoreCase));

        if (exists)
        {
            throw new DomainException($"الخيار \"{value.Trim()}\" موجود بالفعل.");
        }
    }

    private static void ValidateType(PropertyType propertyType)
    {
        if (!Enum.IsDefined(propertyType))
        {
            throw new DomainException("نوع الخاصية غير صالح.");
        }
    }

    public void UpdateDefinition(
        string name,
        PropertyType propertyType,
        string description,
        IReadOnlyCollection<PropertyOptionChange> options)
    {
        ValidateName(name);
        ValidateType(propertyType);

        var changes = options ?? [];

        if (propertyType == PropertyType.List && changes.Count == 0)
        {
            throw new DomainException("خاصية القائمة يجب أن تحتوي على خيار واحد على الأقل.");
        }

        if (propertyType != PropertyType.List && changes.Count > 0)
        {
            throw new DomainException("الخيارات متاحة لخصائص القائمة فقط.");
        }

        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var retainedIds = new HashSet<int>();

        foreach (var change in changes)
        {
            ValidateOptionValue(change.Value);

            if (!values.Add(change.Value.Trim()))
            {
                throw new DomainException("لا يمكن تكرار قيم الخيارات.");
            }

            if (change.Id is int id && (id <= 0 || !retainedIds.Add(id) || GetOption(id) is null))
            {
                throw new DomainException("أحد الخيارات لا ينتمي إلى هذه الخاصية أو تم تكراره.");
            }
        }

        // Validate the complete proposed definition before changing any aggregate state.
        Name = name.Trim();
        PropertyType = propertyType;
        Description = description?.Trim();
        _options.RemoveAll(option => !retainedIds.Contains(option.Id));

        foreach (var change in changes.Where(change => change.Id.HasValue))
        {
            GetOption(change.Id.Value).Rename(change.Value.Trim());
        }

        foreach (var change in changes.Where(change => !change.Id.HasValue))
        {
            _options.Add(PropertyOption.Create(change.Value.Trim()));
        }
    }
}
