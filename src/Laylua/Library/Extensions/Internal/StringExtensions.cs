using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Qommon;

namespace Laylua;

internal static partial class StringExtensions
{
    [GeneratedRegex("[^a-zA-Z0-9_]+", RegexOptions.Compiled)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static partial Regex GetInvalidCharsRegex();

    [GeneratedRegex("^\\d", RegexOptions.Compiled)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static partial Regex GetStartsWithDigitsRegex();

    public static string ValidateIdentifier(this string input)
    {
        Guard.IsNotNullOrWhiteSpace(input);

        var startsWithDigitsRegex = GetStartsWithDigitsRegex();
        if (startsWithDigitsRegex.IsMatch(input))
            throw new ArgumentException($"'{input}' is not a valid Lua identifier: must not start with a digit.");

        var invalidCharsRegex = GetInvalidCharsRegex();
        if (invalidCharsRegex.IsMatch(input))
            throw new ArgumentException($"'{input}' is not a valid Lua identifier: only alphanumeric characters are allowed.");

        return input;
    }

    public static string ToIdentifier(this string input)
    {
        Guard.IsNotNullOrWhiteSpace(input);

        var startsWithDigitsRegex = GetStartsWithDigitsRegex();
        if (startsWithDigitsRegex.IsMatch(input))
            throw new ArgumentException($"'{input}' is not a valid Lua identifier: must not start with a digit.");

        var invalidCharsRegex = GetInvalidCharsRegex();
        var output = invalidCharsRegex.Replace(input.Replace(".", "__"), "");
        if (string.IsNullOrWhiteSpace(output))
            throw new ArgumentException($"'{input}' is not a valid Lua identifier: only alphanumeric characters are allowed.");

        return output;
    }

    public static object SingleQuoted(this object value)
    {
        return $"'{value}'";
    }

    public static object SingleQuoted(this IEnumerable<object> values)
    {
        return string.Join(", ", values.Select(static value => value.SingleQuoted()));
    }

    public static bool ToPath(this string input, [MaybeNullWhen(false)] out string[] path)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentNullException(nameof(input));

        var split = input.Split('.', StringSplitOptions.TrimEntries);
        if (split.Length != 1)
        {
            path = split;
            return true;
        }

        path = default;
        return false;
    }
}
