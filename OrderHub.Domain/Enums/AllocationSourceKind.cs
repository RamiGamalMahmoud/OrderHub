using System.ComponentModel;

namespace OrderHub.Domain.Enums;

public enum AllocationSourceKind
{
    [Description("مخزن")]
    Warehouse,

    [Description("مورد")]
    Supplier
}
