using System.Windows;
using System.Windows.Controls;

namespace OrderHub.UI.Features.Orders.Index
{
    internal partial class View : UserControl
    {
        private bool _isLoaded;

        public View(ViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            Loaded += View_Loaded;
        }

        private void View_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModel viewModel && !_isLoaded)
            {
                Dispatcher.Invoke(() => viewModel.LoadCommand.ExecuteAsync(null));
                _isLoaded = true;
            }
        }
    }
}
