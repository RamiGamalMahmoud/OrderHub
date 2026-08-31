using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Services;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ShippingCarriersDtos;

namespace OrderHub.UI.Features.ShippingCarriers.Edit;

public class ViewModel : Editor.ViewModel, IParameterizedViewModel
{
    private int _shippingCarriersId;
    public ViewModel(IMediator mediator) : base(mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        await base.LoadAsync();
        ShippingCarrierEditDto dto = await _mediator.Send(new Application.Queries.ShippingCarriersQueries.GetShippingCarrierForEditQuery(_shippingCarriersId));
        if (dto is null)
        {
            NotificationService.Instance.ShowError("خطأ! لم يتم تحميل بيانات شركة الشحن للتعديل.");
            await Task.Delay(3000);
            OnRequestClose();
            return;
        }

        Name = dto.Name;
        ShippingCostText = dto.ShippingCost.ToString();

        SelectedCity = Cities.Where(c => c.Id == dto.CityId).SingleOrDefault();
        Street = dto.Street;

        CountryCode = dto.CountryCode;
        PhoneNumber = dto.PhoneNumber;

        HasChanges = false;

    }

    public override string Title => "تعديل بيانات شركة شحن";

    protected override async Task Save()
    {
        decimal.TryParse(ShippingCostText, out decimal shippingCost);
        ShippingCarrierUpdateDto dto = new ShippingCarrierUpdateDto(_shippingCarriersId, Name, shippingCost, CountryCode, PhoneNumber, SelectedCity.Id, Street);
        Result result = await _mediator.Send(new Application.Commands.ShippingCarriersCommands.UpdateShippingCarrierCommand(dto));
        if (result.IsSuccess)
        {
            string message = MessageBuilder.Build(MessageBuilder.OperationType.Update, true, "شركة شحن");
            NotificationService.Instance.ShowSuccess(message);
            WeakReferenceMessenger.Default.Send(new Application.Messages.ShippingCarriers.ShippingCarrierUpdatedMessage());
            OnRequestClose();
        }
        else
        {
            string message = MessageBuilder.Build(MessageBuilder.OperationType.Update, false, "شركة شحن");
            NotificationService.Instance.ShowError(message);
        }
    }

    public Task Initialize(object parameter)
    {
        if(parameter is not null && parameter is int id)
        {
            _shippingCarriersId = id;
        }
        return Task.CompletedTask;
    }
}
