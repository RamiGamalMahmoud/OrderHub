using System.Windows.Controls;

namespace OrderHub.UI.Features.Clients.Edit
{
    public abstract partial class View : UserControl
    {
        public View(ViewModelBase viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            Loaded += (s, e) => Dispatcher.Invoke(() => viewModel.LoadAsync());
        }
    }
}
