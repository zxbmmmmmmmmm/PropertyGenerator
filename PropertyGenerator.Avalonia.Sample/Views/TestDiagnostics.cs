using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;

namespace PropertyGenerator.Avalonia.Sample.Views;

    // ===== Attached Property Diagnostics =====

    // PGA1006: Invalid attached property names
    [GeneratedAttachedProperty<TestAttachedPropertyInvalidNames, string>("")]
    [GeneratedAttachedProperty<TestAttachedPropertyInvalidNames, string>("1NotAnIdentifier")]
    public partial class TestAttachedPropertyInvalidNames : AvaloniaObject
    {
    }

    // PGA1002 (CS0311): Non-AvaloniaObject host type (caught by compiler generic constraint)
    [GeneratedAttachedProperty<TestNonAvaloniaObject, string>("TestNonAvaloniaObjectProp")]
    public partial class TestNonAvaloniaObject
    {
    }

    // PGA1008: Duplicate attached property name
    [GeneratedAttachedProperty<AvaloniaObject, string>("TestDuplicatedProp")]
    [GeneratedAttachedProperty<AvaloniaObject, string>("TestDuplicatedProp")]
    public partial class TestDuplicated
    {
    }

    // PGA1007: Containing type not partial (attached)
    [GeneratedAttachedProperty<AvaloniaObject, string>("SilentAttachedText", DefaultValue = "silent")]
    public class TestNoPartial
    {
    }

    // ===== Styled Property Diagnostics =====

    // PGA1007: Containing type not partial (styled)
    public class TestStyledNotPartial : AvaloniaObject
    {
        [GeneratedStyledProperty]
        public partial int Foo { get; set; }
    }

    // PGA1002: Containing type does not inherit AvaloniaObject (styled)
    public partial class TestStyledNotAvaloniaObject
    {
        [GeneratedStyledProperty]
        public partial int Bar { get; set; }
    }

    // PGA1003: Referenced Validate method not found (styled)
    public partial class TestStyledValidateNotFound : AvaloniaObject
    {
        [GeneratedStyledProperty(Validate = nameof(Object))]
        public partial int Count { get; set; }
    }

    // PGA1003: Referenced Coerce method not found (styled)
    public partial class TestStyledCoerceNotFound : AvaloniaObject
    {
        [GeneratedStyledProperty(Coerce = "NonExistentCoerce")]
        public partial int Count { get; set; }
    }

    // PGA1004: Referenced Validate method has wrong signature (styled)
    public partial class TestStyledValidateBadSignature : AvaloniaObject
    {
        [GeneratedStyledProperty(Validate = nameof(BadValidate))]
        public partial int Count { get; set; }

        // Wrong: returns void instead of bool
        private static void BadValidate(int value) { }
    }

    // PGA1004: Referenced Coerce method has wrong signature (styled)
    public partial class TestStyledCoerceBadSignature : AvaloniaObject
    {
        [GeneratedStyledProperty(Coerce = nameof(BadCoerce))]
        public partial int Count { get; set; }

        // Wrong: only 1 parameter instead of 2
        private static int BadCoerce(int value) => value;
    }

    // ===== Direct Property Diagnostics =====

    // PGA1007: Containing type not partial (direct)
    public class TestDirectNotPartial : AvaloniaObject
    {
        [GeneratedDirectProperty]
        public partial int Baz { get; set; }
    }

    // PGA1002: Containing type does not inherit AvaloniaObject (direct)
    public partial class TestDirectNotAvaloniaObject
    {
        [GeneratedDirectProperty]
        public partial int Qux { get; set; }
    }

    // PGA1005: Setter specified without Getter (direct)
    public partial class TestDirectSetterWithoutGetter : AvaloniaObject
    {
        [GeneratedDirectProperty(Setter = nameof(MySetter))]
        public partial int Value { get; set; }

        public static void MySetter(TestDirectSetterWithoutGetter o, int v) => _ = v;
    }

    // PGA1003: Referenced Getter method not found (direct)
    public partial class TestDirectGetterNotFound : AvaloniaObject
    {
        [GeneratedDirectProperty(Getter = "NonExistentGetter")]
        public partial int Value { get; set; }
    }

    // PGA1004: Referenced Getter method has wrong signature (direct)
    public partial class TestDirectGetterBadSignature : AvaloniaObject
    {
        [GeneratedDirectProperty(Getter = nameof(BadGetter))]
        public partial int Value { get; set; }

        // Wrong: takes no parameters (should take TOwner)
        public static int BadGetter() => 0;
    }

    // PGA1003: Referenced Setter method not found (direct)
    public partial class TestDirectSetterNotFound : AvaloniaObject
    {
        [GeneratedDirectProperty(Getter = nameof(MyGetter), Setter = "NonExistentSetter")]
        public partial int Value { get; set; }

        public static int MyGetter(TestDirectSetterNotFound o) => o.Value;
    }

    // PGA1004: Referenced Setter method has wrong signature (direct)
    public partial class TestDirectSetterBadSignature : AvaloniaObject
    {
        [GeneratedDirectProperty(Getter = nameof(MyGetter), Setter = nameof(BadSetter))]
        public partial int Value { get; set; }

        public static int MyGetter(TestDirectSetterBadSignature o) => o.Value;

        // Wrong: returns int instead of void
        public static int BadSetter(TestDirectSetterBadSignature o, int v) => v;
    }

    // PGA1003: Referenced Coerce method not found (direct)
    public partial class TestDirectCoerceNotFound : AvaloniaObject
    {
        [GeneratedDirectProperty(Coerce = "NonExistentCoerce")]
        public partial int Value { get; set; }
    }

    // ===== OnPropertyChanged Diagnostics =====

    // PGA1009: Target property not found for GenerateOnPropertyChanged
    [GenerateOnPropertyChanged("NonExistentProperty")]
    public partial class TestOnChangedTargetNotFound : AvaloniaObject
    {
    }

    // PGA1010: GenerateOnPropertyChanged disabled by DoNotGenerateOnPropertyChanged
    [GenerateOnPropertyChanged("SomeProperty")]
    [DoNotGenerateOnPropertyChanged]
    public partial class TestOnChangedDisabled : AvaloniaObject
    {
    }
