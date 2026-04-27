namespace Laylua.Marshaling;

/// <summary>
///     Describes the marshaling compatibility for binding a Lua value to a .NET target type,
///     including the conversion kind, cost, and characteristic flags.
/// </summary>
public readonly struct LuaMarshalingCompatibility
{
    /// <summary>
    ///     A default instance representing an incompatible binding.
    /// </summary>
    public static readonly LuaMarshalingCompatibility Incompatible = default;

    /// <summary>
    ///     Gets the kind of conversion this compatibility result represents.
    /// </summary>
    public LuaMarshalingCompatibilityKind Kind { get; }

    /// <summary>
    ///     Gets the cost of this conversion. Lower values indicate cheaper (more preferred) conversions.
    /// </summary>
    public ushort Cost { get; }

    /// <summary>
    ///     Gets the flags describing additional characteristics of this compatibility result.
    /// </summary>
    public LuaMarshalingCompatibilityFlags Flags { get; }

    /// <summary>
    ///     Gets whether this result represents a compatible binding.
    /// </summary>
    public bool IsCompatible => Kind != LuaMarshalingCompatibilityKind.None;

    /// <summary>
    ///     Initializes a new <see cref="LuaMarshalingCompatibility"/> with the specified kind, cost, and flags.
    /// </summary>
    /// <param name="kind">The kind of conversion.</param>
    /// <param name="cost">The cost of the conversion.</param>
    /// <param name="flags">Optional flags describing additional characteristics.</param>
    public LuaMarshalingCompatibility(
        LuaMarshalingCompatibilityKind kind,
        ushort cost,
        LuaMarshalingCompatibilityFlags flags = LuaMarshalingCompatibilityFlags.None)
    {
        Kind = kind;
        Cost = cost;
        Flags = flags;
    }

    /// <summary>
    ///     Initializes a new <see cref="LuaMarshalingCompatibility"/> with the specified kind and flags,
    ///     using the kind's numeric value as the default cost.
    /// </summary>
    public LuaMarshalingCompatibility(
        LuaMarshalingCompatibilityKind kind,
        LuaMarshalingCompatibilityFlags flags = LuaMarshalingCompatibilityFlags.None)
    {
        Kind = kind;
        Cost = (ushort) kind;
        Flags = flags;
    }

    /// <summary>
    ///     Returns whether this compatibility result is allowed by the specified implicit conversion policy.
    /// </summary>
    /// <remarks>
    ///     This method bridges the marshaler's raw capability declaration with the handler's policy filtering.
    ///     A compatible result may still be disallowed if the required conversion category is not enabled in the policy.
    /// </remarks>
    /// <param name="policy">The implicit conversion policy to check against.</param>
    /// <returns>
    ///     <see langword="true"/> if this result is compatible and permitted by the policy;
    ///     <see langword="false"/> otherwise.
    /// </returns>
    public bool IsAllowedBy(LuaAllowedValueConversions policy)
    {
        if (!IsCompatible)
        {
            return false;
        }

        var requiredPolicy = GetRequiredPolicy(Kind, Flags);
        if ((policy & requiredPolicy) != requiredPolicy)
        {
            return false;
        }

        return true;
    }

    private static LuaAllowedValueConversions GetRequiredPolicy(
        LuaMarshalingCompatibilityKind kind,
        LuaMarshalingCompatibilityFlags flags)
    {
        var requiredPolicy = kind switch
        {
            LuaMarshalingCompatibilityKind.FunctionToMethodInfo => LuaAllowedValueConversions.FunctionToCallableTarget,
            LuaMarshalingCompatibilityKind.FunctionToDelegate => LuaAllowedValueConversions.FunctionToCallableTarget,
            LuaMarshalingCompatibilityKind.TableMaterialization => LuaAllowedValueConversions.TableToObject,
            LuaMarshalingCompatibilityKind.StringToPrimitive => LuaAllowedValueConversions.StringToPrimitive,
            LuaMarshalingCompatibilityKind.StringToChar => LuaAllowedValueConversions.StringToChar,
            LuaMarshalingCompatibilityKind.StringToEnum => LuaAllowedValueConversions.StringToEnum,
            LuaMarshalingCompatibilityKind.NumberToEnum => LuaAllowedValueConversions.NumberToEnum,
            LuaMarshalingCompatibilityKind.PrimitiveToString => LuaAllowedValueConversions.PrimitiveToString,
            LuaMarshalingCompatibilityKind.ObjectFallback => LuaAllowedValueConversions.ObjectFallback,
            _ => LuaAllowedValueConversions.None,
        };

        if ((flags & LuaMarshalingCompatibilityFlags.UsesDelegateBinding) != 0)
        {
            requiredPolicy |= LuaAllowedValueConversions.FunctionToCallableTarget;
        }

        if ((flags & LuaMarshalingCompatibilityFlags.UsesTableMaterialization) != 0)
        {
            requiredPolicy |= LuaAllowedValueConversions.TableToObject;
        }

        if ((flags & LuaMarshalingCompatibilityFlags.UsesObjectFallback) != 0)
        {
            requiredPolicy |= LuaAllowedValueConversions.ObjectFallback;
        }

        if ((flags & LuaMarshalingCompatibilityFlags.UsesStringParsing) != 0)
        {
            requiredPolicy |= LuaAllowedValueConversions.StringToPrimitive;
        }

        if ((flags & LuaMarshalingCompatibilityFlags.UsesPrimitiveToString) != 0)
        {
            requiredPolicy |= LuaAllowedValueConversions.PrimitiveToString;
        }

        if ((flags & LuaMarshalingCompatibilityFlags.UsesStringToChar) != 0)
        {
            requiredPolicy |= LuaAllowedValueConversions.StringToChar;
        }

        if ((flags & LuaMarshalingCompatibilityFlags.UsesStringToEnum) != 0)
        {
            requiredPolicy |= LuaAllowedValueConversions.StringToEnum;
        }

        if ((flags & LuaMarshalingCompatibilityFlags.UsesNumberToEnum) != 0)
        {
            requiredPolicy |= LuaAllowedValueConversions.NumberToEnum;
        }

        return requiredPolicy;
    }

    /// <summary>
    ///     Creates a compatibility result for an exact primitive match.
    /// </summary>
    /// <param name="flags">Optional flags describing additional characteristics.</param>
    /// <returns>A new <see cref="LuaMarshalingCompatibility"/> with the <see cref="LuaMarshalingCompatibilityKind.ExactPrimitive"/> kind.</returns>
    public static LuaMarshalingCompatibility ExactPrimitive(
        LuaMarshalingCompatibilityFlags flags = LuaMarshalingCompatibilityFlags.None)
    {
        return new LuaMarshalingCompatibility(LuaMarshalingCompatibilityKind.ExactPrimitive, flags);
    }

    /// <summary>
    ///     Creates a compatibility result for an exact Lua reference match.
    /// </summary>
    /// <returns>A new <see cref="LuaMarshalingCompatibility"/> with the <see cref="LuaMarshalingCompatibilityKind.ExactLuaReference"/> kind.</returns>
    public static LuaMarshalingCompatibility ExactLuaReference()
    {
        return new LuaMarshalingCompatibility(LuaMarshalingCompatibilityKind.ExactLuaReference);
    }

    /// <summary>
    ///     Creates a compatibility result for a Lua reference wrapped in a <c>LuaWeakReference&lt;T&gt;</c>.
    /// </summary>
    /// <returns>
    ///     A new <see cref="LuaMarshalingCompatibility"/> with the <see cref="LuaMarshalingCompatibilityKind.ExactLuaReference"/> kind
    ///     and <see cref="LuaMarshalingCompatibilityFlags.UsesWeakReferenceWrapper"/> flag.
    /// </returns>
    public static LuaMarshalingCompatibility LuaReferenceWrapper()
    {
        return new LuaMarshalingCompatibility(
            LuaMarshalingCompatibilityKind.ExactLuaReference,
            LuaMarshalingCompatibilityFlags.UsesWeakReferenceWrapper);
    }
}
