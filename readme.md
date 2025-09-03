# PropertyGenerator.Avalonia

[![NuGet Version](https://img.shields.io/nuget/vpre/PropertyGenerator.Avalonia)](https://www.nuget.org/packages/PropertyGenerator.Avalonia)
[![NuGet Downloads](https://img.shields.io/nuget/dt/PropertyGenerator.Avalonia)](https://www.nuget.org/packages/PropertyGenerator.Avalonia)

Auto generate `StyledProperty` and `DirectProperty` for Avalonia applications

## StyledProperty

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

## DirectProperty

Similar in usage to `StyledProperty` generator

```csharp
[GeneratedDirectProperty]
public partial IEnumerable? Items { get; set; }
```

***

You can directly initialize `DirectProperty`
```csharp
[GeneratedDirectProperty]
public partial IEnumerable? Items { get; set; } = new AvaloniaList<object>();
```


***

You can also customize `Getter` and `Setter` for `DirectProperty`

```csharp
[GeneratedDirectProperty(Getter = nameof(Getter), Setter = nameof(Setter))]
public partial IEnumerable? Items { get; set; }
public static IEnumerable? Getter(MainWindow o) => o.Items;
public static void Setter(MainWindow o, IEnumerable? v) => o.Items = v;
```

Generated code:

```csharp
public static readonly DirectProperty<MainWindow, IEnumerable?> ItemsProperty
    = AvaloniaProperty.RegisterDirect<MainWindow, IEnumerable?>(
    name: nameof(Items),
    getter: Getter, 
    setter: Setter);

public partial IEnumerable? Items { get => field; set => SetAndRaise(ItemsProperty, ref field, value); }
```

## OnPropertyChanged

By default, the generator will override `OnPropertyChanged` and generate property changed methods at the same time:

```csharp
partial void OnCountPropertyChanged(int newValue);
partial void OnCountPropertyChanged(int oldValue, int newValue);
partial void OnCountPropertyChanged(AvaloniaPropertyChangedEventArgs e);

partial void OnPropertyChangedOverride(AvaloniaPropertyChangedEventArgs change);

protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
{
    base.OnPropertyChanged(change);
    switch (change.Property.Name)
    {
        case nameof(Count):
            OnCountPropertyChanged(change);
            OnCountPropertyChanged((int)change.NewValue);
            OnCountPropertyChanged((int)change.OldValue, (int)change.NewValue);
            break;
		OnPropertyChangedOverride(change);
    }
}
```
You can still handle all property changes by using `OnPropertyChangedOverride` method.

***

To generate property changed methods for properties in base class, you can use `GenerateOnPropertyChangedAttribute` on target class:
```csharp
[GenerateOnPropertyChanged(nameof(Height))]
public partial class MainWindow : Window
{ ... }
```

***

To disable this feature, use `DoNotGenerateOnPropertyChangedAttribute` for class or assembly:

```csharp
[DoNotGenerateOnPropertyChanged]
public partial class MainWindow : Window
{ ... }
```
```csharp
[assembly: DoNotGenerateOnPropertyChanged]
```
