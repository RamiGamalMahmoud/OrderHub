using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OrderHub.UI.Common.Controls;

public partial class Paginator : UserControl
{
    public Paginator()
    {
        InitializeComponent();
    }

    // =========================
    // Summary
    // =========================

    public string Summary
    {
        get => (string)GetValue(SummaryProperty);
        set => SetValue(SummaryProperty, value);
    }

    public static readonly DependencyProperty SummaryProperty =
        DependencyProperty.Register(
            nameof(Summary),
            typeof(string),
            typeof(Paginator),
            new PropertyMetadata(string.Empty));

    // =========================
    // Next Command
    // =========================

    public ICommand NextCommand
    {
        get => (ICommand)GetValue(NextCommandProperty);
        set => SetValue(NextCommandProperty, value);
    }

    public static readonly DependencyProperty NextCommandProperty =
        DependencyProperty.Register(
            nameof(NextCommand),
            typeof(ICommand),
            typeof(Paginator),
            new PropertyMetadata(null));

    // =========================
    // Previous Command
    // =========================

    public ICommand PreviousCommand
    {
        get => (ICommand)GetValue(PreviousCommandProperty);
        set => SetValue(PreviousCommandProperty, value);
    }

    public static readonly DependencyProperty PreviousCommandProperty =
        DependencyProperty.Register(
            nameof(PreviousCommand),
            typeof(ICommand),
            typeof(Paginator),
            new PropertyMetadata(null));
}