using OrderHub.UI.Interfaces;
using System.Windows;

namespace OrderHub.UI.Features.Clients.Edit
{
    public abstract partial class View : Window, IDialog
    {
        public View(ViewModelBase viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            Loaded += (s, e) => Dispatcher.Invoke(() => viewModel.LoadAsync());
            viewModel.RequestClose += () => Close();
        }

        public new void Show() => ShowDialog();

        private void Button_Click(object sender, RoutedEventArgs e) => Close();
    }
}
