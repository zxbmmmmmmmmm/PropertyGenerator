# PropertyGenerator.Avalonia

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
```csharp
[GeneratedDirectProperty(1)]
public partial int Count { get; set; }
```
***

Due to the inability of `partial property` to be initialized directly, `GenerateredDirectProperty` still needs to use `DefaultValueCallback` to complete complex initialization operations

```csharp
[GeneratedDirectProperty(DefaultValueCallback = nameof(DefaultValueCallback))]
public partial IEnumerable? Items { get; set; }
private static IEnumerable? DefaultValueCallback() => new AvaloniaList<AvaloniaObject>();
```

Generated code:

```csharp
public static readonly DirectProperty<MainWindow, IEnumerable?> ItemsProperty =
    AvaloniaProperty.RegisterDirect<MainWindow, IEnumerable?>(
    name: nameof(Items2), 
    getter: o => o.Items2, 
    setter: (o, v) => o.Items2 = v);

public partial IEnumerable? Items { get => field; set => SetAndRaise(ItemsProperty, ref field, value); } = DefaultValueCallback();
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

By default, the generator will override`OnPropertyChanged` and generate partial method at the same time:

```csharp
partial void OnCountPropertyChanged(int newValue);
partial void OnCountPropertyChanged(int oldValue, int newValue);
partial void OnCountPropertyChanged(AvaloniaPropertyChangedEventArgs e);

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
    }
}
```

To disable this feature, please add `DoNotGenerateOnPropertyChangedAttribute`

```csharp
[DoNotGenerateOnPropertyChanged]
public partial class MainWindow : Window
{ ... }
```

You can also disable this feature on the entire assembly:

```csharp
[assembly: DoNotGenerateOnPropertyChanged]
```
