using CommunityToolkit.Mvvm.ComponentModel;
using OrderHub.Application.Common.Extensions;
using OrderHub.Application.Common.Lookups;
using OrderHub.Application.Interfaces;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Orders.Editor;

internal partial class OrderPartyPanelViewModel : ObservableValidator
{
    private readonly ILookupService _lookupService;

    public OrderPartyPanelViewModel(ILookupService lookupService)
    {

        ValidateAllProperties();
        _lookupService = lookupService;
    }

    [ObservableProperty]
    private IEnumerable<ClientLookup> _clients;

    [ObservableProperty]
    private IEnumerable<PaymentMethodLookup> _paymentMethods;

    [ObservableProperty]
    private PaymentMethodLookup _selectedPaymentMethod;

    [ObservableProperty]
    [Required(ErrorMessage = "العميل مطلوب")]
    [NotifyDataErrorInfo]
    private ClientLookup _selectedClient;

    public async Task LoadAsync()
    {
        PaymentMethods = await _lookupService.GetPaymentMethods();
        Clients = (await _lookupService.GetClients()).MergeSelectedItem(SelectedClient);
    }

}