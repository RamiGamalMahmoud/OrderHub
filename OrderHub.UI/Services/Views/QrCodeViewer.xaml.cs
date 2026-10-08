using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace OrderHub.UI.Services.Views;

public partial class QrCodeViewer : UserControl
{
    public QrCodeViewer(BitmapImage image)
    {
        InitializeComponent();
        Image = image;
        DataContext = this;
    }

    public BitmapImage Image { get; }
}
