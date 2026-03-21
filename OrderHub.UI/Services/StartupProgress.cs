using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace OrderHub.UI.Services;

public partial class StartupProgress : ObservableObject
{
    [ObservableProperty]
    private double _value;

    [ObservableProperty]
    private string _message = "جاري تهيئة التطبيق...";

    public void Report(double value, string message)
    {
        if (System.Windows.Application.Current?.Dispatcher?.CheckAccess() == true)
        {
            Value = value;
            Message = message;
            return;
        }

        System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
        {
            Value = value;
            Message = message;
        });
    }
}
