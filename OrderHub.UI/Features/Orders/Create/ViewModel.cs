using MediatR;

namespace OrderHub.UI.Features.Orders.Create;

internal class ViewModel : Editor.ViewModel
{
    public ViewModel(IMediator mediator) : base(mediator)
    {
    }
}
