using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace OrderHub.UI.Common;

public partial class PaginationViewModel : ObservableObject
{
    public event EventHandler PageChanged;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoPrevious))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(Summary))]
    [NotifyCanExecuteChangedFor(nameof(MoveNextCommand))]
    [NotifyCanExecuteChangedFor(nameof(MovePreviousCommand))]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _pageSize = 25;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(Summary))]
    [NotifyCanExecuteChangedFor(nameof(MoveNextCommand))]
    private int _totalPages;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    private int _totalCount;

    public bool CanGoPrevious =>
        CurrentPage > 1;

    public bool CanGoNext =>
        CurrentPage < TotalPages;

    public string Summary =>
        TotalCount == 0
            ? "لا توجد بيانات"
            : $"الصفحة {CurrentPage} من {TotalPages} - إجمالي {TotalCount}";

    [RelayCommand]
    public void Reset()
    {
        CurrentPage = 1;
    }

    public void Update(
        int pageNumber,
        int totalPages,
        int totalCount)
    {
        CurrentPage = pageNumber;
        TotalPages = totalPages;
        TotalCount = totalCount;
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void MoveNext()
    {
        if (!CanGoNext)
            return;

        CurrentPage++;

        OnPageChanged();
    }

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private void MovePrevious()
    {
        if (!CanGoPrevious)
            return;

        CurrentPage--;

        OnPageChanged();
    }

    private void OnPageChanged()
    {
        PageChanged?.Invoke(
            this,
            EventArgs.Empty);
    }
}