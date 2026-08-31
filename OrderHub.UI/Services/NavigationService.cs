using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace OrderHub.UI.Services;

public class NavigationService : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private object _currentView = null;

    public static NavigationService Instance { get; private set; }

    public static void Init(IServiceProvider serviceProvider)
    {
        Instance ??= new NavigationService(serviceProvider);
    }
    
    private NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public TView NavigateTo<TView>() where TView : class
    {
        TView view = _serviceProvider.GetRequiredService<TView>();

        CurrentView = view;
        return view;
    }

    public object CurrentView
    {
        get => _currentView;
        private set => SetProperty(ref _currentView, value);
    }
}
