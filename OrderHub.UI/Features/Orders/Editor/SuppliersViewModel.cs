using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using static OrderHub.Application.DTOs.CommonDtos;

namespace OrderHub.UI.Features.Orders.Editor;

internal partial class SuppliersViewModel : ObservableObject
{
    [ObservableProperty]
    private IEnumerable<SupplierInfoDto> _suppliers = [];

    [ObservableProperty]
    private IEnumerable<SupplierInfoDto> _productSuppliers = [];

    [ObservableProperty]
    private SupplierInfoDto _selectedSupplier;
}
