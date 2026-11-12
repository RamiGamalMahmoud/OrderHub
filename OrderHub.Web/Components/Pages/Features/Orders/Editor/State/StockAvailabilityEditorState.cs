namespace OrderHub.Web.Components.Pages.Features.Orders.Editor.State;

public sealed record StockAvailabilityEditorState(
    int WarehouseId,
    string WarehouseName,
    int StockOnHand,
    int ReservedOnHand,
    int Available);
