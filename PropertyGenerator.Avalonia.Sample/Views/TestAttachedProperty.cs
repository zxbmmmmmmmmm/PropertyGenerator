using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace PropertyGenerator.Avalonia.Sample.Views;

[GenerateAttachedProperty<TestAttachedProperty, string>("AttachedTestProp")]
[GenerateAttachedProperty<TestAttachedProperty, int>("AttachedIntProp", DefaultValue = 114514)]
[GenerateAttachedProperty<TestAttachedProperty, bool>("AttachedWithOptions",
    DefaultValueCallback = nameof(GetDefaultAttachedWithOptions),
    Validate = nameof(ValidateAttachedWithOptions),
    Coerce = nameof(CoerceAttachedWithOptions),
    Inherits = true,
    DefaultBindingMode = BindingMode.TwoWay)]
[GenerateAttachedProperty<Control, double>("AttachedOpacity", DefaultValue = 1.0)]
[GenerateAttachedProperty<TestAttachedProperty, string>("DuplicateAttachedName")]
[GenerateAttachedProperty<TestAttachedProperty, string>("DuplicateAttachedName", DefaultValue = "duplicate")]
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

    private static bool CoerceAttachedWithOptions(TestAttachedProperty host, bool value)
    {
        _ = host;
        return value;
    }

    static partial void OnAttachedTestPropPropertyChanged(TestAttachedProperty host, AvaloniaPropertyChangedEventArgs e)
    {
        _ = host;
        _ = e;
    }

    static partial void OnAttachedTestPropPropertyChanged(TestAttachedProperty host, string newValue)
    {
        _ = host;
        _ = newValue;
    }

    static partial void OnAttachedTestPropPropertyChanged(TestAttachedProperty host, string oldValue, string newValue)
    {
        _ = host;
        _ = oldValue;
        _ = newValue;
    }

    static partial void OnAttachedIntPropPropertyChanged(TestAttachedProperty host, AvaloniaPropertyChangedEventArgs e)
    {
        _ = host;
        _ = e;
    }

    static partial void OnAttachedIntPropPropertyChanged(TestAttachedProperty host, int newValue)
    {
        _ = host;
        _ = newValue;
    }

    static partial void OnAttachedIntPropPropertyChanged(TestAttachedProperty host, int oldValue, int newValue)
    {
        _ = host;
        _ = oldValue;
        _ = newValue;
    }
}

[DoNotGenerateOnPropertyChanged]
[GenerateAttachedProperty<TestAttachedPropertyWithoutOnChanged, string>("SilentAttachedText", DefaultValue = "silent")]
[GenerateAttachedProperty<TestAttachedPropertyWithoutOnChanged, int>("SilentAttachedNumber", DefaultValue = 10, Inherits = true)]
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
