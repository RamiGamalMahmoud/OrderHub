using System;
using System.Diagnostics;
using System.Windows;
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

    private void AttachmentDropZone_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;

        e.Handled = true;
    }

    private void AttachmentDropZone_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        var files = (string[])e.Data.GetData(DataFormats.FileDrop);

        foreach (var file in files)
        {
            if(DataContext is SendMessageViewModel viewModel)
            {
                viewModel.AddFiles(files);
            }
            Debug.WriteLine(file);
        }
    }
}
