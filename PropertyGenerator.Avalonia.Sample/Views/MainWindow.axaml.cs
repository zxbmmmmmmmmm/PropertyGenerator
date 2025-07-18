using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using PropertyGenerator.Avalonia;

namespace PropertyGenerator.Avalonia.Sample.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        CountProperty.Changed.AddClassHandler<MainWindow>((s,e) =>
        {
        });
    }

    [GeneratedStyledProperty]
    public partial int Count { get; set; }

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