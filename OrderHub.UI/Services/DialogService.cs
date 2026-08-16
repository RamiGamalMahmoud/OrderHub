using Microsoft.Extensions.DependencyInjection;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using System;
using System.Windows.Controls;

internal sealed class DialogService
{
    public static DialogService Instance { get; private set; } = null!;
    public static void Init(IServiceProvider serviceProvider)
    {
        if (Instance is null)
            Instance = new DialogService(serviceProvider);
    }

    private readonly IServiceProvider _serviceProvider;

    private DialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void ShowDialog<TView>(object parameter = null)
        where TView : IDialog
    {
        var view = _serviceProvider.GetRequiredService<TView>();

        if (view is Control control &&
            control.DataContext is IParameterizedViewModel viewModel)
        {
            viewModel.Initialize(parameter);
        }

        view.Show();
    }

    public bool Confirm(string message)
    {
        var confirmView = new OrderHub.UI.Features.Confirm.View(message);

        return confirmView.ShowDialog() is true;
    }
}