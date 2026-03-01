using System;
using System.ComponentModel;
using System.Linq;

namespace OrderHub.Domain.Enums;

public enum DeliveryMethod
{
    [Description("استلام من المقر")]
    Pickup = 1,

    [Description("مندوب توصيل")]
    DeliveryMan = 2,

    [Description("شركة شحن")]
    ShippingCompany = 3
}

public static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = field?
            .GetCustomAttributes(typeof(DescriptionAttribute), false)
            .FirstOrDefault() as DescriptionAttribute;

        return attribute?.Description ?? value.ToString();
    }
}

public class EnumItem<T>
{
    public T Value { get; set; }
    public string DisplayName { get; set; }
}
