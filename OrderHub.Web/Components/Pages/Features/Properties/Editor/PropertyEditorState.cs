using OrderHub.Application.Features.Setup.Properties.Create;
using OrderHub.Application.Features.Setup.Properties.Get;
using OrderHub.Application.Features.Setup.Properties.Update;
using OrderHub.Domain.Enums;

namespace OrderHub.Web.Components.Pages.Features.Properties.Editor;

public sealed class PropertyEditorState
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public PropertyType Type { get; set; } = PropertyType.Text;

    public List<PropertyOptionEditorState> Options { get; } = [];

    public static PropertyEditorState FromDetails(PropertyDetails property)
    {
        var state = new PropertyEditorState
        {
            Name = property.Name,
            Description = property.Description,
            Type = property.PropertyType
        };

        state.Options.AddRange(property.Options.Select(option => new PropertyOptionEditorState
        {
            Id = option.Id,
            Value = option.Value
        }));

        return state;
    }

    public CreatePropertyCommand CreateCommand() => new(
        Name,
        Type,
        Description ?? string.Empty,
        Type == PropertyType.List
            ? Options.Select(option => new PropertyOptionInput(null, option.Value)).ToList()
            : []);

    public UpdatePropertyCommand UpdateCommand(int propertyId) => new(new(
        propertyId,
        Name,
        Description ?? string.Empty,
        Type,
        Type == PropertyType.List
            ? Options.Select(option => new PropertyOptionUpdate(option.Id, option.Value)).ToList()
            : []));

    public string? ValidateOptions()
    {
        if (Type != PropertyType.List)
        {
            return null;
        }

        if (Options.Count == 0)
        {
            return "خاصية القائمة يجب أن تحتوي على خيار واحد على الأقل.";
        }

        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var option in Options)
        {
            string? error = ValidateText(option.Value);

            if (error is not null)
            {
                return error;
            }

            if (!values.Add(option.Value.Trim()))
            {
                return "لا يمكن تكرار قيم الخيارات.";
            }
        }

        return null;
    }

    public static string? ValidateText(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 100)
        {
            return "القيمة مطلوبة ويجب ألا تتجاوز 100 حرف.";
        }

        return null;
    }

    public static string TypeLabel(PropertyType type) => type switch
    {
        PropertyType.Text => "حقل نصي",
        PropertyType.Boolean => "منطقي",
        PropertyType.List => "قائمة اختيارات",
        _ => "نوع غير معروف"
    };
}
