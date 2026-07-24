using System.Windows.Controls;
using System.Windows.Input;

namespace OrderHub.UI.Features.Settings.Properties.PropertyEditor;

public partial class PropertyEditorView : UserControl
{
    public PropertyEditorView()
    {
        InitializeComponent();
    }

    private void TextBox_KeyDown(object sender, KeyEventArgs e)
    {
        string optionValue = (sender as TextBox).Text;
        if(e.Key == Key.Enter)
        {
            var vm = DataContext as PropertyEditorViewModelBase;
            if(vm.AddOptionCommand.CanExecute(null))
            {
                vm.AddOptionCommand.Execute(null);
            }
        }
    }
}
