using System.Collections.Generic;
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
    
    [GeneratedStyledProperty(
        DefaultValueCallback = nameof(DefaultValueCallback),
    DefaultValue = true,
    Validate = nameof(Validate),
    Coerce = nameof(Coerce),
    EnableDataValidation = true,
    Inherits = true)]
    public partial bool? IsStarted { get; set; }


    /// <summary>
    /// IsCapable StyledProperty definition
    /// </summary>
    public static readonly StyledProperty<bool> IsCapableProperty =
        AvaloniaProperty.Register<MainWindow, bool>(nameof(IsCapable), DefaultValueCallback());

    /// <summary>
    /// Gets or sets the IsCapable property. This StyledProperty 
    /// indicates ....
    /// </summary>
    public bool IsCapable
    {
        get => this.GetValue(IsCapableProperty);
        set => SetValue(IsCapableProperty, value);
    }


    [GeneratedStyledProperty]
    public partial List<bool?> IsStarted2 { get; set; }

    private static bool Validate(bool? value)
    {
        return true;
    }
    
    private static bool? Coerce(AvaloniaObject x,bool? y)
    {
        return y;
    }

    private static bool DefaultValueCallback()
    {
        return true;
    }

}