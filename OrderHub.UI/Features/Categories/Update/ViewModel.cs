using MediatR;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Categories.Update;

public class ViewModel : Editor.ViewModel
{
    public ViewModel(IMediator mediator) : base(mediator)
    {
    }

    public override string Title { get; }

    protected override Task Save()
    {
        throw new System.NotImplementedException();
    }
}
