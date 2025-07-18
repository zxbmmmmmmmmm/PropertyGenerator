# PropertyGenerator.Avalonia

Auto generate `StyledProperty` for Avalonia applications

## Usage

```csharp
[GeneratedStyledProperty]
public partial int Count { get; set; }
```

Generated code:

```csharp
StyledProperty<int> CountProperty = AvaloniaProperty.Register<MainWindow, int>(name: nameof(Count));
public partial int Count { get => GetValue(CountProperty); set => SetValue(CountProperty, value); }
```

***

```csharp
[GeneratedStyledProperty(10)]
public partial int Count { get; set; }
```

Generated code:

```csharp
Avalonia.StyledProperty<int> CountProperty = AvaloniaProperty.Register<MainWindow, int>(name: nameof(Count), defaultValue: 10);
public partial int Count { get => GetValue(CountProperty); set => SetValue(CountProperty, value); }
```

***

```csharp
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
```

Generated code:

```csharp
StyledProperty<bool?> IsStartedProperty = AvaloniaProperty.Register<MainWindow, bool?>(
	name: nameof(IsStarted), 
	defaultValue: DefaultValueCallback(), 
	validate: Validate,
	coerce: Coerce, 
	enableDataValidation: true,
	inherits: true, 
	defaultBindingMode:BindingMode.TwoWay);
public partial bool? IsStarted { get => GetValue(IsStartedProperty); set => SetValue(IsStartedProperty, value); }
```

***

By default, the generator will override`OnPropertyChanged` at the same time:

```csharp
partial void OnCountPropertyChanged(int newValue);
partial void OnCountPropertyChanged(int oldValue, int newValue);
partial void OnCountPropertyChanged(global::Avalonia.AvaloniaPropertyChangedEventArgs e);
protected override void OnPropertyChanged(global::Avalonia.AvaloniaPropertyChangedEventArgs change)
{
    base.OnPropertyChanged(change);
    switch (change.Property.Name)
    {
        case nameof(Count):
            OnCountPropertyChanged(change);
            OnCountPropertyChanged((int)change.NewValue);
            OnCountPropertyChanged((int)change.OldValue, (int)change.NewValue);
            break;
    }
}
```

To disable this feature, please add `DoNotGenerateOnPropertyChangedAttribute`

```csharp
[DoNotGenerateOnPropertyChanged]
public partial class MainWindow : Window
{ ... }
```

