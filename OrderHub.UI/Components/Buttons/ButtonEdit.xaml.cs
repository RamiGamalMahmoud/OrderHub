using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace OrderHub.UI.Components.Buttons;

public partial class ButtonEdit : UserControl
{
    public ButtonEdit()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(ButtonEdit));

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(ButtonEdit));

    public object CommandParameter
    {
        get => (object)GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }
}
