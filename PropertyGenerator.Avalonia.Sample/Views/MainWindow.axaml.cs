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
        IsRunningProperty.Changed.AddClassHandler<MainWindow>((s,e) =>
        {
        });
    }

    [GeneratedStyledProperty]
    public partial bool? IsRunning { get; set; }

    [GeneratedStyledProperty(DefaultValueCallback = nameof(DefaultValueCallback),DefaultValue = true, Validate = nameof(Validate),Coerce = nameof(Coerce),EnableDataValidation = true,Inherits = true, DefaultBindingMode = BindingMode.TwoWay)]
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
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        switch (change.Property.Name)
        {
            case nameof(IsRunning):
                OnIsRunningChanged((bool?)change.OldValue, (bool?)change.NewValue);
                break;
            case nameof(IsStarted):
                OnIsStartingChanged((bool?)change.OldValue, (bool?)change.NewValue);
                break;
        }
    }
    private partial void OnIsRunningChanged(bool? oldValue, bool? newValue);
    private partial void OnIsStartingChanged(bool? oldValue, bool? newValue);

}