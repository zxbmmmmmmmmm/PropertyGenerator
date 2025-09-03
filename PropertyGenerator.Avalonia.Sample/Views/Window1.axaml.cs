using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PropertyGenerator.Avalonia.Sample.Views;

[GenerateOnPropertyChanged(nameof(Height))]
public partial class Window1 : Window
{
    public Window1()
    {
        InitializeComponent();
    }

    partial void OnHeightPropertyChanged(double newValue)
    {

    }
}
