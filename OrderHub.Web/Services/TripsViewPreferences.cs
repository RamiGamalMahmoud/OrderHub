namespace OrderHub.Web.Services;

/// <summary>
/// Per-circuit memory for each trips view's rows-per-page choice.
/// Kept separate per view so changing one table never resizes another.
/// </summary>
public sealed class TripsViewPreferences
{
    public int ListPageSize { get; set; } = 25;

    public int HistoryPageSize { get; set; } = 10;

    public int DeliverymanStandalonePageSize { get; set; } = 10;

    public int DeliverymanOrderBoundPageSize { get; set; } = 10;

    public static int Normalize(int value, int fallback) =>
        value is 10 or 25 or 50 ? value : fallback;
}
