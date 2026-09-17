using OrderHub.UI.Messaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.UI.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.WhatsappGroups.Index;

internal partial class WhatsappGroupsViewModel : IndexViewModelBase<WhatsappGroupViewModel>
{
    private List<WhatsappGroupViewModel> _allWhatsappGroups = [];

    public WhatsappGroupsViewModel(IMediator mediator) : base(mediator)
    {
        WeakReferenceMessenger.Default.Register<WhatsAppGroupsMessages.WhatsappGroupCreatedMessage>(this, async (r, m) => await LoadAsync());
        WeakReferenceMessenger.Default.Register<WhatsAppGroupsMessages.WhatsappGroupUpdatedMessage>(this, async (r, m) => await LoadAsync());
        WeakReferenceMessenger.Default.Register<WhatsAppGroupsMessages.WhatsappGroupDeletedMessage>(this, async (r, m) => await LoadAsync());
    }

    protected override async Task LoadAsync()
    {
        var dtos = await _mediator.Send(new Application.Queries.WhatsappGroupQueries.GetAllWhatsappGroupListsQuery());
        _allWhatsappGroups = dtos.Select(WhatsappGroupViewModel.FromDto).ToList();
        ApplyFilter();
    }

    protected override async Task ReloadAsync()
    {
        var dtos = await _mediator.Send(new Application.Queries.WhatsappGroupQueries.GetAllWhatsappGroupListsQuery());
        _allWhatsappGroups = dtos.Select(WhatsappGroupViewModel.FromDto).ToList();
        ApplyFilter();
    }

    private IEnumerable<WhatsappGroupViewModel> _whatsappGroupViewModels;
    public IEnumerable<WhatsappGroupViewModel> WhatsappGroups { get => _whatsappGroupViewModels; private set => SetProperty(ref _whatsappGroupViewModels, value); }

    [ObservableProperty]
    private string _searchTerm;

    partial void OnSearchTermChanged(string oldValue, string newValue) => ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<WhatsappGroupViewModel> filtered = _allWhatsappGroups;

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            string term = SearchTerm.Trim();
            filtered = filtered.Where(whatsappGroup =>
                whatsappGroup.Name?.Contains(term) == true);
        }

        WhatsappGroups = filtered.ToList();
    }
}
