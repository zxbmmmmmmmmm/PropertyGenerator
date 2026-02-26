using Avalonia;

namespace PropertyGenerator.Avalonia.Sample.Views;

public class TestNestedClass
{
    partial class NestedStyled : AvaloniaObject
    {
        [GeneratedStyledProperty]
        public partial int Foo { get; set; }
    }
    
    [GenerateAttachedProperty<NestedAttached, string>("Bar")]
    partial class NestedAttached : AvaloniaObject
    {
        public NestedAttached()
        {
        }
    }
}
