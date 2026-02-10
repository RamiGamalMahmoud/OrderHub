using System.Windows;

namespace OrderHub.UI.Features.Deliverymen.Editor
{
    public partial class View : Window
    {
        public View(ViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.RequestClose += () => Close();
            Loaded += async (_, _) => await Dispatcher.Invoke(viewModel.InitializeAsync);
            Closed += (_, _) => viewModel.Dispose();
        }
    }
}
