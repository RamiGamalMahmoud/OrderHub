using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace OrderHub.UI.Services;

public sealed partial class NavigationService : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly List<object> _history = [];

    private object _currentView;
    private int _currentIndex = -1;

    public static NavigationService Instance { get; private set; } = null!;

    private NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public static void Init(IServiceProvider serviceProvider)
    {
        Instance ??= new NavigationService(serviceProvider);
    }

    public event EventHandler NavigationChanged;

    public object CurrentView
    {
        get => _currentView;
        private set => SetProperty(ref _currentView, value);
    }

    public bool CanGoBack => _currentIndex > 0;

    public bool CanGoForward =>
        _currentIndex >= 0 &&
        _currentIndex < _history.Count - 1;

    public int HistoryCount => _history.Count;

    public TView NavigateTo<TView>() where TView : class
    {
        TView view = _serviceProvider.GetRequiredService<TView>();

        Navigate(view);

        return view;
    }

    public void GoBack()
    {
        if (!CanGoBack)
            return;

        _currentIndex--;

        SetCurrentView(_history[_currentIndex]);
    }

    public void GoForward()
    {
        if (!CanGoForward)
            return;

        _currentIndex++;

        SetCurrentView(_history[_currentIndex]);
    }

    public void Reset()
    {
        _history.Clear();
        _currentIndex = -1;

        CurrentView = null;

        NotifyNavigationStateChanged();
    }

    private void Navigate(object view)
    {
        ArgumentNullException.ThrowIfNull(view);

        RemoveForwardHistory();

        _history.Add(view);
        _currentIndex = _history.Count - 1;

        SetCurrentView(view);
    }

    private void RemoveForwardHistory()
    {
        int forwardStartIndex = _currentIndex + 1;

        if (forwardStartIndex >= _history.Count)
            return;

        _history.RemoveRange(
            forwardStartIndex,
            _history.Count - forwardStartIndex);
    }

    private void SetCurrentView(object view)
    {
        CurrentView = view;

        NotifyNavigationStateChanged();
    }

    private void NotifyNavigationStateChanged()
    {
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoForward));
        OnPropertyChanged(nameof(HistoryCount));

        NavigationChanged?.Invoke(this, EventArgs.Empty);
    }
}