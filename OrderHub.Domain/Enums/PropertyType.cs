using System.ComponentModel;

namespace OrderHub.Domain.Enums;

public enum PropertyType
{
    [Description("حقل نصي")]
    Text,

    [Description("منطقي")]
    Boolean,

    [Description("قائمة اختيارات")]
    List
}
