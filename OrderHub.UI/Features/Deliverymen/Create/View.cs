using OrderHub.UI.Interfaces;

namespace OrderHub.UI.Features.Deliverymen.Create;

internal class View : Editor.View, IDialog
{
    public View(ViewModel viewModel) : base(viewModel)
    {
    }
}
