using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using OrderHub.UI.Stores.Markers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.WhatsappGroups.Index;

internal partial class WhatsappGroupsViewModel : IndexViewModelBase<WhatsappGroupViewModel>
{
    private readonly ISelectionStore<IWhatsappGroupMarker, int> _selectionStore;
    private List<WhatsappGroupViewModel> _allWhatsappGroups = [];

    public WhatsappGroupsViewModel(IMediator mediator, ISelectionStore<IWhatsappGroupMarker, int> selectionStore) : base(mediator)
    {
        _selectionStore = selectionStore;

        WeakReferenceMessenger.Default.Register<Application.Messages.WhatsappGroups.WhatsappGroupCreatedMessage>(this, async (r, m) => await LoadAsync());
        WeakReferenceMessenger.Default.Register<Application.Messages.WhatsappGroups.WhatsappGroupUpdatedMessage>(this, async (r, m) => await LoadAsync());
        WeakReferenceMessenger.Default.Register<Application.Messages.WhatsappGroups.WhatsappGroupDeletedMessage>(this, async (r, m) => await LoadAsync());
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

    protected override Task ShowEditAsync(WhatsappGroupViewModel viewModel)
    {
        _selectionStore.Id = viewModel.Id;
        DialogService.Instance.ShowDialog<Update.View>();
        return Task.CompletedTask;
    }

    protected override async Task DeleteAsync(WhatsappGroupViewModel viewModel)
    {
        if (DialogService.Instance.Confirm($"هل تريد حذف مجموعة الواتساب {viewModel.Name}"))
        {
            Result result = await _mediator.Send(new Application.Commands.WhatsappGroupCommands.DeleteWhatsappGroupCommand(viewModel.Id));
            if(result.IsSuccess)
            {
                await _mediator.Publish(new Application.Notifications.SuccessNotification(MessageBuilder.Build(MessageBuilder.OperationType.Delete, true, "مجموعة الواتساب")));
                WeakReferenceMessenger.Default.Send(new Application.Messages.WhatsappGroups.WhatsappGroupDeletedMessage());
                await LoadAsync();
            }
            else
            {
                await _mediator.Publish(new Application.Notifications.ErrorNotification(MessageBuilder.Build(MessageBuilder.OperationType.Delete, false, "مجموعة الواتساب")));
            }
        }
    }

    protected override Task ShowCreateAsync()
    {
        DialogService.Instance.ShowDialog<Create.View>();
        return Task.CompletedTask;
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
