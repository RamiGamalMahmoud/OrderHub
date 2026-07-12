using OrderHub.UI.Interfaces;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OrderHub.UI.Features.Orders.Editor;

internal partial class View : Window, IDialog
{
    public View(ViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += async (_, _) => await viewModel.LoadAsync();
        viewModel.RequestClose += () => Close();
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.F1)
        {
            e.Handled = true;
            TabControl.SelectedIndex = 0;
        }

        if (e.Key == Key.F2)
        {
            e.Handled = true;
            TabControl.SelectedIndex = 1;
        }

        if (e.Key == Key.F12)
        {
            ViewModel viewModel = DataContext as ViewModel;
            if (viewModel.SaveCommand.CanExecute(null))
                viewModel.SaveCommand.Execute(null);
        }

        //if (e.Key == Key.Enter)
        //{
        //    if (Keyboard.FocusedElement is not Button)
        //    {
        //        e.Handled = true;

        //        var request = new TraversalRequest(FocusNavigationDirection.Next);
        //        if (Keyboard.FocusedElement is UIElement elementWithFocus)
        //        {
        //            elementWithFocus.MoveFocus(request);
        //        }
        //    }
        //}
    }
}
