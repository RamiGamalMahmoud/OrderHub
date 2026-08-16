using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using OrderHub.UI.Stores.Markers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.DeliverymanDtos;

namespace OrderHub.UI.Features.Deliverymen.Index;

public partial class ViewModel : IndexViewModelBase<DeliverymanListDto>
{
    private readonly ISelectionStore<IDeliverymanMarker, int> _selectionStore;
    private IEnumerable<DeliverymanListDto> _deliverymen;
    private List<DeliverymanListDto> _allDeliverymen = [];

    public ViewModel(IMediator mediator, ISelectionStore<IDeliverymanMarker, int> selectionStore) : base(mediator)
    {
        _selectionStore = selectionStore;

        WeakReferenceMessenger.Default.Register<Application.Messages.Deliveryman.DeliverymanCreatedMessage>(this, async (r, m) => await ReloadAsync());
        WeakReferenceMessenger.Default.Register<Application.Messages.Deliveryman.DeleverymanUpdateMessage>(this, async (r, m) => await ReloadAsync());
        WeakReferenceMessenger.Default.Register<Application.Messages.Deliveryman.DeliverymanDeletedMessage>(this, async (r, m) => await ReloadAsync());
    }

    protected override async Task LoadAsync()
    {
        _allDeliverymen = (await _mediator.Send(new Application.Queries.DeliverymanQueries.GetAllDeliverymenListQuery())).ToList();
        ApplyFilter();
    }

    protected override async Task ReloadAsync()
    {
        _allDeliverymen = (await _mediator.Send(new Application.Queries.DeliverymanQueries.GetAllDeliverymenListQuery())).ToList();
        ApplyFilter();
    }

    protected override async Task DeleteAsync(DeliverymanListDto deliveryman)
    {
        if (DialogService.Instance.Confirm($"هل تريد حذف المندوب ({deliveryman.Name})؟") is not true)
        {
            return;
        }
        Result result = await _mediator.Send(new Application.Commands.DeliverymanCommands.DeleteDeliverymanCommand(deliveryman.Id));

        if (result.IsSuccess)
        {
            await _mediator.Publish(new Application.Notifications.SuccessNotification("تم حذف المندوب بنجاح."));
            WeakReferenceMessenger.Default.Send(new Application.Messages.Deliveryman.DeliverymanDeletedMessage());
        }
        else
        {
            await _mediator.Publish(new Application.Notifications.ErrorNotification("حدث خطأ أثناء حذف المندوب."));
        }
    }

    protected override Task ShowEditAsync(DeliverymanListDto deliveryman)
    {
        _selectionStore.Id = deliveryman.Id;
        DialogService.Instance.ShowDialog<Edit.View>();
        return Task.CompletedTask;
    }

    protected override Task ShowCreateAsync()
    {
        DialogService.Instance.ShowDialog<Create.View>();
        return Task.CompletedTask;
    }

    public IEnumerable<DeliverymanListDto> Deliverymen
    {
        get => _deliverymen;
        set => SetProperty(ref _deliverymen, value);
    }

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string _searchTerm;

    partial void OnSearchTermChanged(string oldValue, string newValue) => ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<DeliverymanListDto> filtered = _allDeliverymen;

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            string term = SearchTerm.Trim();
            filtered = filtered.Where(deliveryman =>
                deliveryman.Name?.Contains(term) == true
                || deliveryman.CityName?.Contains(term) == true
                || deliveryman.PhoneNumber?.Contains(term) == true
                || deliveryman.WhatsappGroupName?.Contains(term) == true);
        }

        Deliverymen = filtered.ToList();
    }
}
