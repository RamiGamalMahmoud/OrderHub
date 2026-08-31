using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Services;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.DeliverymanDtos;

namespace OrderHub.UI.Features.Deliverymen.Edit;

internal class ViewModel : Editor.ViewModel, IParameterizedViewModel
{
    private int _deliverymanId;
    public ViewModel(IMediator mediator) : base(mediator)
    {
    }

    public override string Title => "تعديل بيانات مندوب";

    public Task Initialize(object parameter)
    {
        if(parameter is not null && parameter is int id)
        {
            _deliverymanId = id;
        }
        return Task.CompletedTask;
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        DeliverymanFormDto deliveryman = await _mediator.Send(new Application.Queries.DeliverymanQueries.GetDeliverymanForEditQuery(_deliverymanId));

        Name = deliveryman.Name;
        SelectedCity = Cities.Where(c => c.Id == deliveryman.CityId).FirstOrDefault();
        PhoneNumber = deliveryman.PhoneNumber;
        SelectedWhatsappGroup = WhatsappGroups.Where(group => group.Id == deliveryman.WhatsappGroupId).FirstOrDefault();
    }

    protected override async Task Save()
    {
        int? whatsappGroupId = SelectedWhatsappGroup?.Id > 0 ? SelectedWhatsappGroup.Id : null;
        DeliverymanFormDto deliveryman = new DeliverymanFormDto(Name, SelectedCity.Id, PhoneNumber, whatsappGroupId);
        Result result = await _mediator.Send(new Application.Commands.DeliverymanCommands.UpdateDeliverymanCommand(_deliverymanId, deliveryman));
        if(result.IsSuccess)
        {
            NotificationService.Instance.ShowSuccess("تم تعديل بيانات المندوب بنجاح.");
            WeakReferenceMessenger.Default.Send(new Application.Messages.Deliveryman.DeleverymanUpdateMessage());
            OnRequestClose();
        }
        else
        {
            NotificationService.Instance.ShowError("خطأ : لم يتم تعديل بيانات المندوب.");
        }
    }
}
