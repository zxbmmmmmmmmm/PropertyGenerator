using System.Collections;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;

namespace PropertyGenerator.Avalonia.Sample.Views;

[GenerateOnPropertyChanged(nameof(Width))]
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// AAA
    /// </summary>
    [GeneratedStyledProperty(114514)]
    internal partial int Count { get; private set; }

    [GeneratedDirectProperty(114514)]
    internal partial int Count1 { get; private set; }

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
    [GeneratedStyledProperty]
    public partial IEnumerable? Items1 { get; set; }

    [GeneratedStyledProperty]
    public partial Easing Easing { get; set; }

    [GeneratedDirectProperty]
    public partial IEnumerable? Items2 { get; set; }


    [GeneratedDirectProperty(Getter = nameof(Getter), Setter = nameof(Setter))]
    public partial IEnumerable? Items { get; set; }

    public static IEnumerable? Getter(MainWindow o) => o.Items;
    public static void Setter(MainWindow o, IEnumerable? v) => o.Items = v;
}
