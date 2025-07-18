using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace PropertyGenerator.Avalonia.Sample.Views;
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        CountProperty.Changed.AddClassHandler<MainWindow>((s,e) =>
        {
        });
        IsStarted = true;
    }

    /// <summary>
    /// AAA
    /// </summary>
    [GeneratedStyledProperty(114514)]
    internal partial int Count { get; private set; }

    /// <summary>
    /// AAA
    /// </summary>
    [GeneratedStyledProperty(
        DefaultValueCallback = nameof(DefaultValueCallback),
        DefaultValue = true,
        Validate = nameof(Validate),
        Coerce = nameof(Coerce),
        EnableDataValidation = true,
        Inherits = true,
        DefaultBindingMode = BindingMode.TwoWay)]
    public partial bool? IsStarted { get; set; }


    private static bool DefaultValueCallback()
    {
        return true;
    }
    private static bool Validate(bool? value)
    {
        return true;
    }
    private static bool? Coerce(AvaloniaObject x, bool? y)
    {
        return true;
    }
}
