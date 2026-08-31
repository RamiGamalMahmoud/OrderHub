using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace OrderHub.UI.Features.Messages.Index;

public partial class MessageDetails : UserControl
{
    public MessageDetails()
    {
        InitializeComponent();
    }

    private void Button_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        var files = (string[])e.Data.GetData(DataFormats.FileDrop);

        foreach (var file in files)
        {
            if (DataContext is ViewModel viewModel)
            {
                viewModel.SelectedMessage.AddAttachments(files.ToList());
            }
            Debug.WriteLine(file);
        }
    }

    private void Button_DragOver(object sender, System.Windows.DragEventArgs e)
    {


        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;

        e.Handled = true;
    }
}
