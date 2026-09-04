using Microsoft.Extensions.DependencyInjection;
using OrderHub.UI.Common;
using OrderHub.UI.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;

internal sealed class DialogService
{
    public static DialogService Instance { get; private set; } = null!;
    private static HandyControl.Controls.Dialog _currentDialog;
    private readonly Stack<DialogContainerView> _dialogHistory = [];
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

    public void ShowDialog(object content, string title)
    {
        if (content is not UserControl userControl)
            return;

        var dialogContainer = new DialogContainerView(userControl, title);
        dialogContainer.Closed += DialogContainer_Closed;

        if (_currentDialog is null)
        {
            _currentDialog = HandyControl.Controls.Dialog.Show(dialogContainer);
        }
        else
        {
            // احتفظ بالمحتوى الحالي
            if (_currentDialog.Content is DialogContainerView currentContainer)
            {
                _dialogHistory.Push(currentContainer);
            }

            // غير المحتوى فقط
            _currentDialog.Content = dialogContainer;
        }
    }

    public async Task ShowDialog<TView>(string title, object parameter = null)
        where TView : class
    {
        var view = _serviceProvider.GetRequiredService<TView>();

        if (view is System.Windows.Window window)
        {
            if (window.DataContext is IParameterizedViewModel viewModel)
            {
                await viewModel.Initialize(parameter);
            }

            window.Show();
            return;
        }

        if (view is UserControl userControl)
        {
            if (userControl.DataContext is IParameterizedViewModel viewModel)
            {
                await viewModel.Initialize(parameter);
            }

            var dialogContainer = new DialogContainerView(userControl, title);
            dialogContainer.Closed += DialogContainer_Closed;

            if (_currentDialog is null)
            {
                _currentDialog = HandyControl.Controls.Dialog.Show(dialogContainer);
            }
            else
            {
                // احتفظ بالمحتوى الحالي
                if (_currentDialog.Content is DialogContainerView currentContainer)
                {
                    _dialogHistory.Push(currentContainer);
                }

                // غير المحتوى فقط
                _currentDialog.Content = dialogContainer;
            }
        }
    }

    private void DialogContainer_Closed(object sender, EventArgs e)
    {
        if (_currentDialog is null)
            return;

        if(sender is DialogContainerView dialogContainer)
        {
            dialogContainer.Closed -= DialogContainer_Closed;
        }

        if (_dialogHistory.Count == 0)
        {
            _currentDialog.Close();
            _currentDialog = null;
            return;
        }

        var previousContainer = _dialogHistory.Pop();

        _currentDialog.Content = previousContainer;
    }

    public bool Confirm(string message)
    {
        var confirmView = new OrderHub.UI.Features.Confirm.View(message);

        return confirmView.ShowDialog() is true;
    }

    public async Task<bool> ConfirmAsync(string message)
    {
        var resultSource = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var viewModel = new OrderHub.UI.Features.Confirm.ConfirmDialogViewModel
        {
            Message = message
        };

        void OnResultSubmitted(bool result)
        {
            resultSource.TrySetResult(result);
        }

        viewModel.ResultSubmitted += OnResultSubmitted;

        var view = new OrderHub.UI.Features.Confirm.ConfirmDialogView
        {
            DataContext = viewModel
        };

        var dialog = HandyControl.Controls.Dialog.Show(view);

        try
        {
            return await resultSource.Task;
        }
        finally
        {
            viewModel.ResultSubmitted -= OnResultSubmitted;

            if (!dialog.IsClosed)
            {
                dialog.Close();
            }
        }
    }
}