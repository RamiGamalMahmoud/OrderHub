using Microsoft.Extensions.DependencyInjection;
using OrderHub.UI.Interfaces;
using System;
using System.Windows;

namespace OrderHub.UI.Services
{
    internal class DialogService : IDialogService
    {
        private readonly IServiceProvider _serviceProvider;

        public DialogService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void ShowDialog<TView>(object parameter = null) where TView : IDialog
        {
            TView view = _serviceProvider.GetRequiredService<TView>();
            view.Show();
        }

        public bool Confirm(string message)
        {
            Features.Confirm.View confirmView = new Features.Confirm.View(message);
            bool? result = confirmView.ShowDialog();
            return result is true;
        }
    }
}
