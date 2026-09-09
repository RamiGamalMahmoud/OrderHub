using System.Windows;

namespace OrderHub.UI.Features.DatabaseConfiguration;

public partial class DatabaseConfigurationView : Window
{
    public DatabaseConfigurationView(DatabaseConfigurationViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += DatabaseConfigurationView_Loaded;
    }

    private void DatabaseConfigurationView_Loaded(object sender, RoutedEventArgs e)
    {
        if(DataContext is  DatabaseConfigurationViewModel viewModel)
        {
            viewModel.Load();
        }
    }
}
