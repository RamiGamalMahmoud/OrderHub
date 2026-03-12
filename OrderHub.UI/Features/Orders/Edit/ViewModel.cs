using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.UI.Interfaces;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Orders.Edit;

internal class ViewModel : Editor.ViewModel
{
    public ViewModel(IMediator mediator, IDialogService dialogService, IMessenger messenger) : base(mediator, dialogService, messenger)
    {
    }

    public override string ActionName => "حفظ التعديلات";
    public override string Title => "تعديل طلب";

    protected override Task Save()
    {
        throw new System.NotImplementedException();
    }
}
