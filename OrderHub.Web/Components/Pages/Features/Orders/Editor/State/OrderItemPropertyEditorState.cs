using OrderHub.Domain.Enums;

namespace OrderHub.Web.Components.Pages.Features.Orders.Editor.State;

public sealed class OrderItemPropertyEditorState
{
    public int PropertyId { get; init; }

    public required string Name { get; init; }

    public bool HasMetadata { get; init; } = true;

    public bool IsRequired { get; init; }

    public PropertyType Type { get; init; }

    public string? Value { get; set; }

    public IReadOnlyList<OrderItemPropertyOptionEditorState> Options { get; init; } = [];

    public string? ValidateValue(string? value)
    {
        if (!HasMetadata)
            return $"تعذر تحميل تعريف الخاصية رقم {PropertyId}. القيمة المحفوظة لم تُحذف.";

        if (string.IsNullOrWhiteSpace(value))
            return IsRequired ? $"{Name}: القيمة مطلوبة." : null;
        return Type switch
        {
            PropertyType.Boolean when value is not ("true" or "false") => $"{Name}: اختر نعم أو لا.",
            PropertyType.List when !Options.Any(option => option.Value == value) => $"{Name}: الاختيار لم يعد متاحاً. اختر قيمة متاحة.",
            _ => null
        };
    }
}
