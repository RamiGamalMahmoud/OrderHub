
using OrderHub.UI.Interfaces;

namespace OrderHub.UI.Features.Deliverymen.Edit;

internal class View : Editor.View, IDialog
{
    public View(ViewModel viewModel) : base(viewModel)
    {
    }
}
