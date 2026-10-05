using OrderHub.UI.Messaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Features.OrderDrafts.Delete;
using OrderHub.Application.Features.OrderDrafts.List;
using OrderHub.Application.Interfaces;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Orders.Index;

internal partial class OrderDraftsDrawerViewModel : ObservableObject
{
    private readonly IRequestExecutor _requestExecutor;

    public ObservableCollection<OrderDraftSummary> Drafts { get; } = [];

    [ObservableProperty]
    private bool _isLoading;

    public OrderDraftsDrawerViewModel(IRequestExecutor requestExecutor)
    {
        _requestExecutor = requestExecutor;

        WeakReferenceMessenger.Default.Register<OrderDraftsDrawerViewModel, OrdersMessages.OrderDraftSavedMessage>(this, (r, m) =>
        {
            OnPropertyChanged(nameof(Drafts));
        });

        WeakReferenceMessenger.Default.Register<OrderDraftsDrawerViewModel, OrdersMessages.OrderDraftDeletedMessage>(this, async (r, m) =>
        {
            await r.LoadAsync();
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

            var drafts = await _requestExecutor.ExecuteAsync(new GetDraftListQuery());

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
    private async Task DeleteAllDrafts()
    {
        bool deleteDraft = DialogService.Instance.Confirm("هل تريد حذف كل المسودات؟");
        if (!deleteDraft)
            return;

        foreach (var draft in Drafts)
        {
            var result = await _requestExecutor.ExecuteAsync(new DeleteDraftCommand(draft.Id));
            if (!result.IsSuccess) throw new System.InvalidOperationException(result.ErrorMessage);
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