using System;
using System.Windows.Controls;

namespace OrderHub.UI.Services;

public partial class DialogContainerView : UserControl
{
    public DialogContainerView(Control control, string title)
    {
        InitializeComponent();
        DataContext = this;
        Control = control;
        Title = title;
    }

    public Control Control { get; private set; }
    public string Title { get; private set; }

    public event EventHandler Closed;

    private void OnClosed()
    {
        Closed?.Invoke(this, EventArgs.Empty);
    }

    private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        OnClosed();
    }
}
