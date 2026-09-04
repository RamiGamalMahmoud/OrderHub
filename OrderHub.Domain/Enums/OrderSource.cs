using System.ComponentModel;

namespace OrderHub.Domain.Enums;
public enum OrderSource
{
    [Description("سلة")]
    Salla = 1,

    [Description("أونلاين")]
    Online
}
