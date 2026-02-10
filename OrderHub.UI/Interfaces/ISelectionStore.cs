namespace OrderHub.UI.Interfaces
{
    public interface ISelectionStore<TMarker, TId>
    {
        TId Id { get; set; }
        void Clear();
    }
}
