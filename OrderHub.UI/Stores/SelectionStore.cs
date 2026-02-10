using OrderHub.UI.Interfaces;

namespace OrderHub.UI.Stores;

internal sealed class SelectionStore<TMarker, TId> : ISelectionStore<TMarker, TId>
{
    public TId Id { get; set; }

    public void Clear() => Id = default;
}
