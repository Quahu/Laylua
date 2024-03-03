using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Laylua;

/// <summary>
///     Represents a comparer that supports Lua-friendly value equality and comparison.
/// </summary>
public class LuaComparer : IEqualityComparer<LuaReference>, IEqualityComparer<IConvertible>, IEqualityComparer<object>, IEqualityComparer,
    IComparer<IConvertible>, IComparer<object>, IComparer
{
    public IFormatProvider FormatProvider { get; set; }

    public LuaComparer(IFormatProvider formatProvider)
    {
        FormatProvider = formatProvider;
    }

    public int IndexOf(object?[] array, object? value)
    {
        return IndexOf(array, value, 0, array.Length);
    }

    public int IndexOf(object?[] array, object? value, int startIndex)
    {
        return IndexOf(array, value, startIndex, array.Length);
    }

    public int IndexOf(object?[] array, object? value, int startIndex, int count)
    {
        var endIndex = startIndex + count;
        for (var i = startIndex; i < endIndex; i++)
        {
            if (Equals(array[i], value))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsNull([NotNullWhen(false)] object? x, [NotNullWhen(false)] object? y, out bool nullEqual)
    {
        if (x == null && y == null)
        {
            nullEqual = true;
            return true;
        }

        if (x == null)
        {
            nullEqual = false;
            return true;
        }

        nullEqual = false;
        return false;
    }

    public bool Equals(LuaReference? x, LuaReference? y)
    {
        if (IsNull(x, y, out var nullEqual))
            return nullEqual;

        return x.Equals(y);
    }

    public bool Equals(IConvertible? x, IConvertible? y)
    {
        if (IsNull(x, y, out var nullEqual))
            return nullEqual;

        var xTypeCode = x.GetTypeCode();
        var yTypeCode = y.GetTypeCode();
        if (xTypeCode == yTypeCode)
            return x.Equals(y);

        switch (xTypeCode)
        {
            case TypeCode.Char:
            {
                var xValue = x.ToChar(FormatProvider);
                switch (yTypeCode)
                {
                    case TypeCode.Char:
                    {
                        var yValue = y.ToChar(FormatProvider);
                        return xValue == yValue;
                    }
                    case TypeCode.String:
                    {
                        var yValue = y.ToString(FormatProvider);
                        return yValue.Length == 1 && yValue[1] == xValue;
                    }
                    default:
                    {
                        return false;
                    }
                }
            }
            case TypeCode.String:
            {
                var xValue = x.ToString(FormatProvider);
                switch (yTypeCode)
                {
                    case TypeCode.Char:
                    {
                        var yValue = y.ToChar(FormatProvider);
                        return xValue.Length == 1 && xValue[1] == yValue;
                    }
                    case TypeCode.String:
                    {
                        var yValue = y.ToString(FormatProvider);
                        return xValue == yValue;
                    }
                    default:
                    {
                        return false;
                    }
                }
            }
            case TypeCode.Boolean:
            {
                var xValue = x.ToBoolean(FormatProvider);
                switch (yTypeCode)
                {
                    case TypeCode.Boolean:
                    {
                        var yValue = y.ToBoolean(FormatProvider);
                        return xValue == yValue;
                    }
                    default:
                    {
                        return false;
                    }
                }
            }
            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
            {
                var xValue = x.ToInt64(FormatProvider);
                switch (yTypeCode)
                {
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    {
                        var yValue = y.ToInt64(FormatProvider);
                        return xValue == yValue;
                    }
                    default:
                    {
                        return false;
                    }
                }
            }
            case TypeCode.Single:
            case TypeCode.Double:
            {
                switch (yTypeCode)
                {
                    case TypeCode.Single:
                    case TypeCode.Double:
                    {
                        var xValue = x.ToDouble(FormatProvider);
                        var yValue = y.ToDouble(FormatProvider);
                        return xValue.Equals(yValue);
                    }
                    case TypeCode.Decimal:
                    {
                        var xValue = x.ToDecimal(FormatProvider);
                        var yValue = y.ToDecimal(FormatProvider);
                        return xValue.Equals(yValue);
                    }
                    default:
                    {
                        return false;
                    }
                }
            }
            case TypeCode.Decimal:
            {
                switch (yTypeCode)
                {
                    case TypeCode.Single:
                    case TypeCode.Double:
                    {
                        var xValue = x.ToDouble(FormatProvider);
                        var yValue = y.ToDouble(FormatProvider);
                        return xValue.Equals(yValue);
                    }
                    case TypeCode.Decimal:
                    {
                        var xValue = x.ToDecimal(FormatProvider);
                        var yValue = y.ToDecimal(FormatProvider);
                        return xValue.Equals(yValue);
                    }
                    default:
                    {
                        return false;
                    }
                }
            }
            default:
            {
                return false;
            }
        }
    }

    public new bool Equals(object? x, object? y)
    {
        if (IsNull(x, y, out var nullEqual))
            return nullEqual;

        if (x is LuaReference xReference && y is LuaReference yReference)
            return Equals(xReference, yReference);

        if (x is IConvertible xConvertible && y is IConvertible yConvertible)
            return Equals(xConvertible, yConvertible);

        return x.Equals(y);
    }

    int IEqualityComparer<LuaReference>.GetHashCode(LuaReference obj)
    {
        return obj.GetHashCode();
    }

    int IEqualityComparer<IConvertible>.GetHashCode(IConvertible obj)
    {
        return obj.GetHashCode();
    }

    int IEqualityComparer<object>.GetHashCode(object obj)
    {
        return obj.GetHashCode();
    }

    int IEqualityComparer.GetHashCode(object obj)
    {
        return obj.GetHashCode();
    }

    public int Compare(IConvertible? x, IConvertible? y)
    {
        if (x == null && y == null)
            return 0;

        if (x == null)
            return 1;

        if (y == null)
            return -1;

        if (x is IComparable xComparable && x.GetType() == y.GetType())
            return xComparable.CompareTo(y);

        var xTypeCode = x.GetTypeCode();
        var yTypeCode = y.GetTypeCode();
        switch (xTypeCode)
        {
            case TypeCode.Char:
            {
                var xValue = x.ToChar(FormatProvider);
                switch (yTypeCode)
                {
                    case TypeCode.Char:
                    {
                        var yValue = y.ToChar(FormatProvider);
                        return xValue.CompareTo(yValue);
                    }
                    case TypeCode.String:
                    {
                        var yValue = y.ToString(FormatProvider);
                        if (yValue.Length == 1)
                            return yValue[1].CompareTo(xValue);

                        break;
                    }
                }

                break;
            }
            case TypeCode.String:
            {
                var xValue = x.ToString(FormatProvider);
                switch (yTypeCode)
                {
                    case TypeCode.Char:
                    {
                        var yValue = y.ToChar(FormatProvider);
                        if (xValue.Length == 1)
                            return xValue[1].CompareTo(yValue);

                        break;
                    }
                    case TypeCode.String:
                    {
                        var yValue = y.ToString(FormatProvider);
                        return string.Compare(xValue, yValue, FormatProvider as CultureInfo, CompareOptions.None);
                    }
                }

                break;
            }
            case TypeCode.Boolean:
            {
                var xValue = x.ToBoolean(FormatProvider);
                switch (yTypeCode)
                {
                    case TypeCode.Boolean:
                    {
                        var yValue = y.ToBoolean(FormatProvider);
                        return xValue.CompareTo(yValue);
                    }
                }

                break;
            }
            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
            {
                var xValue = x.ToInt64(FormatProvider);
                switch (yTypeCode)
                {
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    {
                        var yValue = y.ToInt64(FormatProvider);
                        return xValue.CompareTo(yValue);
                    }
                }

                break;
            }
            case TypeCode.Single:
            case TypeCode.Double:
            {
                switch (yTypeCode)
                {
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    {
                        var xValue = x.ToDouble(FormatProvider);
                        var yValue = y.ToInt64(FormatProvider);
                        return xValue.CompareTo(yValue);
                    }
                    case TypeCode.Single:
                    case TypeCode.Double:
                    {
                        var xValue = x.ToDouble(FormatProvider);
                        var yValue = y.ToDouble(FormatProvider);
                        return xValue.CompareTo(yValue);
                    }
                    case TypeCode.Decimal:
                    {
                        var xValue = x.ToDecimal(FormatProvider);
                        var yValue = y.ToDecimal(FormatProvider);
                        return xValue.CompareTo(yValue);
                    }
                }

                break;
            }
            case TypeCode.Decimal:
            {
                switch (yTypeCode)
                {
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    {
                        var xValue = x.ToDecimal(FormatProvider);
                        var yValue = y.ToInt64(FormatProvider);
                        return xValue.CompareTo(yValue);
                    }
                    case TypeCode.Single:
                    case TypeCode.Double:
                    {
                        var xValue = x.ToDouble(FormatProvider);
                        var yValue = y.ToDouble(FormatProvider);
                        return xValue.CompareTo(yValue);
                    }
                    case TypeCode.Decimal:
                    {
                        var xValue = x.ToDecimal(FormatProvider);
                        var yValue = y.ToDecimal(FormatProvider);
                        return xValue.CompareTo(yValue);
                    }
                }

                break;
            }
        }

        return Comparer.Default.Compare(x, y);
    }

    public int Compare(object? x, object? y)
    {
        if (x == null && y == null)
            return 0;

        if (x == null)
            return 1;

        if (y == null)
            return -1;

        if (x is IComparable xComparable && x.GetType() == y.GetType())
            return xComparable.CompareTo(y);

        if (x is IConvertible xConvertible && y is IConvertible yConvertible)
            return Compare(xConvertible, yConvertible);

        return Comparer.Default.Compare(x, y);
    }
}
