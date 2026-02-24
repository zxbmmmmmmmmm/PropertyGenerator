using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace PropertyGenerator.Avalonia.Sample.Views;

[GeneratedAttachedProperty<TestAttachedProperty, string>("AttachedTestProp")]
[GeneratedAttachedProperty<TestAttachedProperty, int>("AttachedIntProp", DefaultValue = 114514)]
[GeneratedAttachedProperty<TestAttachedProperty, bool>("AttachedWithOptions",
    DefaultValueCallback = nameof(GetDefaultAttachedWithOptions),
    Validate = nameof(ValidateAttachedWithOptions),
    Coerce = nameof(CoerceAttachedWithOptions),
    Inherits = true,
    DefaultBindingMode = BindingMode.TwoWay)]
[GeneratedAttachedProperty<Control, double>("AttachedOpacity", DefaultValue = 1.0)]
[GeneratedAttachedProperty<TestAttachedProperty, string>("DuplicateAttachedName")]
[GeneratedAttachedProperty<TestAttachedProperty, string>("DuplicateAttachedName", DefaultValue = "duplicate")]
public partial class TestAttachedProperty : AvaloniaObject
{
    string ExerciseBasic()
    {
        SetAttachedTestProp(this, "114514");
        return GetAttachedTestProp(this);
    }

    int ExerciseValueType()
    {
        SetAttachedIntProp(this, 1919810);
        return GetAttachedIntProp(this);
    }

    bool ExerciseAdvancedOptions()
    {
        SetAttachedWithOptions(this, false);
        return GetAttachedWithOptions(this);
    }

    double ExerciseDifferentHost(Control control)
    {
        SetAttachedOpacity(control, 0.5);
        return GetAttachedOpacity(control);
    }

    string ExerciseDuplicateNameCase()
    {
        SetDuplicateAttachedName(this, "duplicate");
        return GetDuplicateAttachedName(this);
    }

    private static bool GetDefaultAttachedWithOptions()
    {
        return true;
    }

    private static bool ValidateAttachedWithOptions(bool value)
    {
        return value;
    }

    private static bool CoerceAttachedWithOptions(AvaloniaObject host, bool value)
    {
        _ = host;
        return value;
    }

    static partial void OnAttachedTestPropPropertyChanged(AvaloniaObject host, AvaloniaPropertyChangedEventArgs e)
    {
        _ = host;
        _ = e;
    }

    static partial void OnAttachedTestPropPropertyChanged(AvaloniaObject host, string newValue)
    {
        _ = host;
        _ = newValue;
    }

    static partial void OnAttachedTestPropPropertyChanged(AvaloniaObject host, string oldValue, string newValue)
    {
        _ = host;
        _ = oldValue;
        _ = newValue;
    }

    static partial void OnAttachedIntPropPropertyChanged(AvaloniaObject host, AvaloniaPropertyChangedEventArgs e)
    {
        _ = host;
        _ = e;
    }

    static partial void OnAttachedIntPropPropertyChanged(AvaloniaObject host, int newValue)
    {
        _ = host;
        _ = newValue;
    }

    static partial void OnAttachedIntPropPropertyChanged(AvaloniaObject host, int oldValue, int newValue)
    {
        _ = host;
        _ = oldValue;
        _ = newValue;
    }
}

[DoNotGenerateOnPropertyChanged]
[GeneratedAttachedProperty<TestAttachedPropertyWithoutOnChanged, string>("SilentAttachedText", DefaultValue = "silent")]
[GeneratedAttachedProperty<TestAttachedPropertyWithoutOnChanged, int>("SilentAttachedNumber", DefaultValue = 10, Inherits = true)]
public partial class TestAttachedPropertyWithoutOnChanged : AvaloniaObject
{
    string ExerciseSilentText()
    {
        SetSilentAttachedText(this, "runtime");
        return GetSilentAttachedText(this);
    }

    int ExerciseSilentNumber()
    {
        SetSilentAttachedNumber(this, 10);
        return GetSilentAttachedNumber(this);
    }
}

