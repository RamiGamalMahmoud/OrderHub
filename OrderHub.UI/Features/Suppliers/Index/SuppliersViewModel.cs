using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.SupplierDtos;

namespace OrderHub.UI.Features.Suppliers.Index;

internal partial class SuppliersViewModel : IndexViewModelBase<SupplierListDto>
{
    private List<SupplierListDto> _allSuppliers = [];

    public SuppliersViewModel(IMediator mediator) : base(mediator)
    {
        WeakReferenceMessenger.Default.Register<Application.Messages.Suppliers.SupplierCreatedMessage>(this, async (r, m) => await LoadAsync());
        WeakReferenceMessenger.Default.Register<Application.Messages.Suppliers.SupplierUpdatedMessage>(this, async (r, m) => await LoadAsync());
        WeakReferenceMessenger.Default.Register<Application.Messages.Suppliers.SupplierDeletedMessage>(this, async (r, m) => await LoadAsync());
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

    protected override async Task ShowEditAsync(SupplierListDto dto)
    {
        await DialogService.Instance.ShowDialog<Update.View>("تعديل طلب", dto.Id);
    }

    protected override async Task DeleteAsync(SupplierListDto dto)
    {
        if (DialogService.Instance.Confirm($"هل تريد حذف المورد {dto.Name}"))
        {
            Result result = await _mediator.Send(new Application.Commands.SupplierCommands.DeleteSupplierCommand(dto.Id));
            if (result.IsSuccess)
            {
                NotificationService.Instance.ShowSuccess(MessageBuilder.Build(MessageBuilder.OperationType.Delete, true, "المورد"));
                WeakReferenceMessenger.Default.Send(new Application.Messages.Suppliers.SupplierDeletedMessage());
                await LoadAsync();
            }
            else
            {
                NotificationService.Instance.ShowError(MessageBuilder.Build(MessageBuilder.OperationType.Delete, false, "المورد"));
            }
        }
    }

    protected override async Task ShowCreateAsync()
    {
        await DialogService.Instance.ShowDialog<Create.View>("إضافة مورد جديد");
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
