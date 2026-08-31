using System.Windows.Controls;

namespace OrderHub.UI.Features.Documents.Preview;

public partial class PreviewDocumentView : UserControl
{
    public PreviewDocumentView(PreviewDocumentViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
