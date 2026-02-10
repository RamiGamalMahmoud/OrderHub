using System.Windows.Controls;

namespace OrderHub.UI.Features.Clients.Index
{
    public partial class View : UserControl
    {
        public View(ViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            Loaded += async (s, e) => await Dispatcher.Invoke(() => viewModel.LoadCommand.ExecuteAsync(null));
        }
    }
}
