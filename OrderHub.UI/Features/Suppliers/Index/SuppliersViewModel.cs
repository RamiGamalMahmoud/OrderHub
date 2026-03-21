using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using OrderHub.UI.Stores.Markers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.SupplierDtos;

namespace OrderHub.UI.Features.Suppliers.Index;

internal partial class SuppliersViewModel : IndexViewModelBase<SupplierListDto>
{
    private readonly IDialogService _dialogService;
    private readonly ISelectionStore<ISupplierMarker, int> _selectionStore;
    private List<SupplierListDto> _allSuppliers = [];

    public SuppliersViewModel(IMediator mediator, IDialogService dialogService, ISelectionStore<ISupplierMarker, int> selectionStore, IMessenger messenger) : base(mediator, messenger)
    {
        _dialogService = dialogService;
        _selectionStore = selectionStore;

        _messenger.Register<Application.Messages.Suppliers.SupplierCreatedMessage>(this, async (r, m) => await LoadAsync());
        _messenger.Register<Application.Messages.Suppliers.SupplierUpdatedMessage>(this, async (r, m) => await LoadAsync());
        _messenger.Register<Application.Messages.Suppliers.SupplierDeletedMessage>(this, async (r, m) => await LoadAsync());
    }

    protected override async Task LoadAsync()
    {
        _allSuppliers = (await _mediator.Send(new Application.Queries.SupplierQueries.GetAllSuppliersQuery())).ToList();
        ApplyFilter();
    }

    protected override async Task ReloadAsync()
    {
        _allSuppliers = (await _mediator.Send(new Application.Queries.SupplierQueries.GetAllSuppliersQuery())).ToList();
        ApplyFilter();
    }

    protected override Task ShowEditAsync(SupplierListDto dto)
    {
        _selectionStore.Id = dto.Id;
        _dialogService.ShowDialog<Update.View>();
        return Task.CompletedTask;
    }

    protected override async Task DeleteAsync(SupplierListDto dto)
    {
        if (_dialogService.Confirm($"هل تريد حذف المورد {dto.Name}"))
        {
            Result result = await _mediator.Send(new Application.Commands.SupplierCommands.DeleteSupplierCommand(dto.Id));
            if(result.IsSuccess)
            {
                await _mediator.Publish(new Application.Notifications.SuccessNotification(MessageBuilder.Build(MessageBuilder.OperationType.Delete, true, "المورد")));
                _messenger.Send(new Application.Messages.Suppliers.SupplierDeletedMessage());
                await LoadAsync();
            }
            else
            {
                await _mediator.Publish(new Application.Notifications.ErrorNotification(MessageBuilder.Build(MessageBuilder.OperationType.Delete, false, "المورد")));
            }
        }
    }

    protected override Task ShowCreateAsync()
    {
        _dialogService.ShowDialog<Create.View>();
        return Task.CompletedTask;
    }

    private IEnumerable<SupplierListDto> _supplierListDtos;
    public IEnumerable<SupplierListDto> Suppliers { get => _supplierListDtos; private set => SetProperty(ref _supplierListDtos, value); }

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string _searchTerm;

    partial void OnSearchTermChanged(string oldValue, string newValue) => ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<SupplierListDto> filtered = _allSuppliers;

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            string term = SearchTerm.Trim();
            filtered = filtered.Where(supplier =>
                supplier.Name?.Contains(term) == true
                || supplier.Address?.Contains(term) == true
                || supplier.PhoneNumber?.Contains(term) == true);
        }

        Suppliers = filtered.ToList();
    }
}
