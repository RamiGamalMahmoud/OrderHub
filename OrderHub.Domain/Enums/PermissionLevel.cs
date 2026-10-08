using System.ComponentModel;

namespace OrderHub.Domain.Enums;

public enum PermissionLevel
{
    [Description("لا شيء")]
    None,

    [Description("قراءة")]
    Read,

    [Description("كتابة")]
    Write
}