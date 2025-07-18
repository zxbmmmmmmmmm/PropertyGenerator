namespace PropertyGenerator.Avalonia.Generator.Models;

internal abstract partial record AvaloniaPropertyDefaultValue
{
    /// <summary>
    /// A <see cref="AvaloniaPropertyDefaultValue"/> type representing a <see langword="null"/> value.
    /// </summary>
    public sealed record Null : AvaloniaPropertyDefaultValue
    {
        /// <summary>
        /// The shared <see cref="Null"/> instance (the type is stateless).
        /// </summary>
        public static Null Instance { get; } = new();

        /// <inheritdoc/>
        public override string ToString()
        {
            return "null";
        }
    }

    /// <summary>
    /// A <see cref="AvaloniaPropertyDefaultValue"/> type representing an explicit <see langword="null"/> value.
    /// </summary>
    /// <remarks>This is used in some scenarios with mismatched metadata types.</remarks>
    public sealed record ExplicitNull : AvaloniaPropertyDefaultValue
    {
        /// <summary>
        /// The shared <see cref="ExplicitNull"/> instance (the type is stateless).
        /// </summary>
        public static ExplicitNull Instance { get; } = new();

        /// <inheritdoc/>
        public override string ToString()
        {
            return "null";
        }
    }

    /// <summary>
    /// A <see cref="AvaloniaPropertyDefaultValue"/> type representing default value for a specific type.
    /// </summary>
    /// <param name="TypeName">The input type name.</param>
    public sealed record Default(string TypeName) : AvaloniaPropertyDefaultValue
    {
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"default({TypeName})";
        }
    }

    /// <summary>
    /// A <see cref="AvaloniaPropertyDefaultValue"/> type representing the special unset value.
    /// </summary>
    public sealed record UnsetValue : AvaloniaPropertyDefaultValue
    {
        /// <inheritdoc/>
        public override string ToString()
        {
            return "global::Avalonia.AvaloniaProperty.UnsetValue";
        }
    }

    /// <summary>
    /// A <see cref="AvaloniaPropertyDefaultValue"/> type representing a constant value.
    /// </summary>
    /// <param name="Value">The constant value.</param>
    public sealed record Constant(TypedConstantInfo Value) : AvaloniaPropertyDefaultValue
    {
        /// <inheritdoc/>
        public override string ToString()
        {
            return Value.ToString();
        }
    }

    /// <summary>
    /// A <see cref="AvaloniaPropertyDefaultValue"/> type representing a callback.
    /// </summary>
    /// <param name="MethodName">The name of the callback method to invoke.</param>
    public sealed record Callback(string MethodName) : AvaloniaPropertyDefaultValue
    {
        /// <inheritdoc/>
        public override string ToString()
        {
            return MethodName;
        }
    }
}
