using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static OrderHub.Application.DTOs.CommonDtos;

namespace OrderHub.UI.Features.Orders.Editor;

internal partial class SuppliersViewModel : ObservableValidator
{
    public SuppliersViewModel()
    {
        ValidateAllProperties();
    }
    [ObservableProperty]
    private IEnumerable<SupplierInfoDto> _suppliers = [];

    [ObservableProperty]
    private IEnumerable<SupplierInfoDto> _productSuppliers = [];

    [ObservableProperty]
    private SupplierInfoDto _selectedSupplier;
}
