using System.Windows.Controls;

namespace OrderHub.UI.Features.Messages.SendMessage;

public partial class SendMessageView : UserControl
{
    public SendMessageView(SendMessageViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
        Loaded += SendMessageView_Loaded;
    }

    private void SendMessageView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SendMessageViewModel viewModel)
        {
            Dispatcher.Invoke(viewModel.LoadAsync);
        }
    }
}
