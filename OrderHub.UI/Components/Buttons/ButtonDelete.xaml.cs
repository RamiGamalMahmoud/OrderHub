using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace OrderHub.UI.Components.Buttons;

public partial class ButtonDelete : UserControl
{
    public ButtonDelete()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(ButtonDelete));

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(ButtonDelete));

    public object CommandParameter
    {
        get => (object)GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }
}
