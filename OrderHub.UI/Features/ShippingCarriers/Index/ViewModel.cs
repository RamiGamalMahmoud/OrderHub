using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ShippingCarriersDtos;

namespace OrderHub.UI.Features.ShippingCarriers.Index;

public partial class ViewModel : IndexViewModelBase<ShippingCarrierListDto>
{
    private IEnumerable<ShippingCarrierListDto> _shippingCarriers;
    private List<ShippingCarrierListDto> _allShippingCarriers = [];

    public ViewModel(IMediator mediator) : base(mediator)
    {
        WeakReferenceMessenger.Default.Register<Application.Messages.ShippingCarriers.ShippingCarrierCreatedMessage>(this, async (_, _) => await ReloadAsync());
        WeakReferenceMessenger.Default.Register<Application.Messages.ShippingCarriers.ShippingCarrierUpdatedMessage>(this, async (_, _) => await ReloadAsync());
        WeakReferenceMessenger.Default.Register<Application.Messages.ShippingCarriers.ShippingCarrierDeletedMessage>(this, async (_, _) => await ReloadAsync());
    }

    protected override async Task DeleteAsync(ShippingCarrierListDto model)
    {
        if(!DialogService.Instance.Confirm("هل تريد حذف شركة الشحن؟")) return;
        Result result = await _mediator.Send(new Application.Commands.ShippingCarriersCommands.DeleteShippingCarrierCommand(model.Id));

        if(result.IsSuccess)
        {
            string message = MessageBuilder.Build(MessageBuilder.OperationType.Delete, true, "شركة شحن");
            NotificationService.Instance.ShowSuccess(message);
            WeakReferenceMessenger.Default.Send(new Application.Messages.ShippingCarriers.ShippingCarrierDeletedMessage());
        }

        else
        {
            string message = MessageBuilder.Build(MessageBuilder.OperationType.Delete, false, "شركة شحن");
            NotificationService.Instance.ShowError(message);
        }
    }

    protected override async Task LoadAsync()
    {
        _allShippingCarriers = (await _mediator.Send(new Application.Queries.ShippingCarriersQueries.GetShippingCarriersQuery())).ToList();
        ApplyFilter();
    }

    protected override async Task ReloadAsync()
    {
        _allShippingCarriers = (await _mediator.Send(new Application.Queries.ShippingCarriersQueries.GetShippingCarriersQuery())).ToList();
        ApplyFilter();
    }

    protected override async Task ShowCreateAsync()
    {
        await DialogService.Instance.ShowDialog<Create.View>("إضافة شركة شحن جديدة");
    }

    protected override async Task ShowEditAsync(ShippingCarrierListDto model)
    {
        await DialogService.Instance.ShowDialog<Edit.View>("تعديل بيانات شركة شحن", model.Id);
    }

    public IEnumerable<ShippingCarrierListDto> ShippingCarriers { get => _shippingCarriers; private set => SetProperty(ref _shippingCarriers, value); }

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string _searchTerm;

    partial void OnSearchTermChanged(string oldValue, string newValue) => ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<ShippingCarrierListDto> filtered = _allShippingCarriers;

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            string term = SearchTerm.Trim();
            filtered = filtered.Where(carrier =>
                carrier.Name?.Contains(term) == true
                || carrier.PhoneNumber?.Contains(term) == true
                || carrier.Address?.Contains(term) == true);
        }

        ShippingCarriers = filtered.ToList();
    }
}
