namespace OrderHub.Application.Features.Inventory.Presentation;

/// <summary>حالة الرصيد المعروضة في شاشات الأرصدة؛ تُشتق من الكميات الفعلية والمحجوزة.</summary>
public enum StockStatusFilter
{
    All = 0,
    Available = 1,
    PartiallyReserved = 2,
    FullyReserved = 3,
    OutOfStock = 4
}

public enum StockSortColumn
{
    Available = 0,
    Physical = 1,
    Reserved = 2,
    Damaged = 3,
    ProductName = 4
}

public static class StockPaging
{
    public const int MaxPageSize = 100;

    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        int normalizedPage = page < 1 ? 1 : page;
        int normalizedSize = pageSize < 1 ? 1 : pageSize > MaxPageSize ? MaxPageSize : pageSize;
        return (normalizedPage, normalizedSize);
    }
}
