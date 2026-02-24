using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;

namespace PropertyGenerator.Avalonia.Sample.Views;

[GeneratedAttachedProperty<TestAttachedProperty, string>("AttachedTestProp")]
public partial class TestAttachedProperty : AvaloniaObject
{
    void Test()
    {
    }
}
