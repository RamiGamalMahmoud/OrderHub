using MediatR;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Orders.Create;

internal class ViewModel : Editor.ViewModel
{
    public ViewModel(IMediator mediator) : base(mediator)
    {
    }

    public override string Title => "إنشاء طلب جديد";

    protected override Task Save()
    {
        throw new System.NotImplementedException();
    }
}
