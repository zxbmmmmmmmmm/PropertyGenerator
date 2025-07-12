using Avalonia;
using Avalonia.Controls;
using PropertyGenerator.Avalonia;

namespace PropertyGenerator.Avalonia.Sample.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    [GeneratedStyledProperty]
    public partial bool IsStarted { get; set; }
}