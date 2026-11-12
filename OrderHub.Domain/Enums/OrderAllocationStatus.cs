using System.ComponentModel;

namespace OrderHub.Domain.Enums;

public enum OrderAllocationStatus
{
    [Description("نشط")]
    Active,

    [Description("ملغى")]
    Cancelled,

    [Description("محرر")]
    Released,

    [Description("مصروف")]
    Consumed
}
