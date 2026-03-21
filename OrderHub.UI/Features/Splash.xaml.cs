using OrderHub.UI.Services;
using System.Windows;

namespace OrderHub.UI.Features
{
    public partial class Splash : Window
    {
        public Splash(StartupProgress startupProgress)
        {
            InitializeComponent();
            DataContext = startupProgress;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}
