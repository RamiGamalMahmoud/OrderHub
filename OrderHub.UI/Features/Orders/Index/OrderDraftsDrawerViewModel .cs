using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Orders.Index;

internal partial class OrderDraftsDrawerViewModel : ObservableObject
{
    private readonly IDraftService _draftService;

    public ObservableCollection<OrderDraftSummary> Drafts { get; } = [];

    [ObservableProperty]
    private bool _isLoading;

    public OrderDraftsDrawerViewModel(IDraftService draftService)
    {
        _draftService = draftService;

        WeakReferenceMessenger.Default.Register<OrderDraftsDrawerViewModel, Messages.DraftSavedMessage>(this, (r, m) =>
        {
            OnPropertyChanged(nameof(Drafts));
        });

        WeakReferenceMessenger.Default.Register<OrderDraftsDrawerViewModel, Messages.DraftDeletedMessage>(this, (r, m) =>
        {
            OnPropertyChanged(nameof(Drafts));
        });
    }

    public async Task LoadAsync()
    {
        if (IsLoading)
            return;

        try
        {
            IsLoading = true;

            Drafts.Clear();

            var drafts = await _draftService.GetAllAsync();

            foreach (var draft in drafts)
            {
                Drafts.Add(draft);
            }
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(Drafts.Count));
        }
    }

    [RelayCommand]
    private async Task DeleteDraft(OrderDraftSummary draft)
    {
        if (draft is null)
            return;

        bool deleteDraft = DialogService.Instance.Confirm("هل تريد حذف هذه المسودة؟");
        if (!deleteDraft)
            return;

        await _draftService.DeleteAsync(
            new DraftContext(draft.Id));

        Drafts.Remove(draft);
        OnPropertyChanged(nameof(Drafts.Count));
    }

    [RelayCommand]
    private async Task DeleteAllDrafts()
    {
        bool deleteDraft = DialogService.Instance.Confirm("هل تريد حذف كل المسودات؟");
        if (!deleteDraft)
            return;

        foreach (var draft in Drafts)
        {
            await _draftService.DeleteAsync(new DraftContext(draft.Id));
        }
        Drafts.Clear();
        OnPropertyChanged(nameof(Drafts.Count));
    }

    [RelayCommand]
    private async Task OpenDraft(OrderDraftSummary draft)
    {
        await DialogService.Instance.ShowDialog<Features.Orders.Create.View>("متابعة إكمال طلب ", draft.Id);
    }
}