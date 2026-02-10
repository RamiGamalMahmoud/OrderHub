using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.UI.Interfaces;
using System;

namespace OrderHub.UI.Services;

public class NavigationService : ObservableObject, INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private object _currentView = null;
    
    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public TView NavigateTo<TView>() where TView : class
    {
        TView view = _serviceProvider.GetRequiredService<TView>();

        CurrentView = _serviceProvider.GetRequiredService<TView>(); ;
        return view;
    }

    public object CurrentView
    {
        get => _currentView;
        private set => SetProperty(ref _currentView, value);
    }
}
