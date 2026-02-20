using System.Windows.Controls;

namespace OrderHub.UI.Features.Categories.Index;

public partial class CategoriesIndexView : UserControl
{
    public CategoriesIndexView()
    {
        InitializeComponent();

        //Loaded += (_, _) => viewModel.LoadCommand.ExecuteAsync(null);
    }
}
