using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Runtime.CompilerServices;

namespace Laylua;

public partial class LuaReference
{
    /// <summary>
    ///     Disposes the specified object
    ///     if it is a <see cref="LuaReference"/>.
    /// </summary>
    /// <param name="possibleReference"> The possible reference. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Dispose<T>(T possibleReference)
        where T : class?
    {
        if (possibleReference is LuaReference reference)
        {
            reference.Dispose();
        }
    }

    /// <summary>
    ///     Disposes the specified objects
    ///     if they are <see cref="LuaReference"/>s.
    /// </summary>
    /// <param name="possibleReferences"> The possible references. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Dispose<T>(T[]? possibleReferences)
        where T : class?
    {
        if (possibleReferences == null)
        {
            return;
        }

        Exception? disposeException = null;
        foreach (var possibleReference in possibleReferences)
        {
            if (possibleReference is LuaReference reference)
            {
                try
                {
                    reference.Dispose();
                }
                catch (Exception ex)
                {
                    disposeException ??= ex;
                }
            }
        }

        if (disposeException != null)
        {
            ExceptionDispatchInfo.Capture(disposeException).Throw();
        }
    }

    /// <summary>
    ///     Disposes the specified objects
    ///     if they are <see cref="LuaReference"/>s.
    /// </summary>
    /// <param name="possibleReferences"> The possible references. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Dispose<T>(List<T>? possibleReferences)
        where T : class?
    {
        if (possibleReferences == null)
        {
            return;
        }

        Exception? disposeException = null;
        var possibleReferenceCount = possibleReferences.Count;
        for (var i = 0; i < possibleReferenceCount; i++)
        {
            var possibleReference = possibleReferences[i];
            if (possibleReference is LuaReference reference)
            {
                try
                {
                    reference.Dispose();
                }
                catch (Exception ex)
                {
                    disposeException ??= ex;
                }
            }
        }

        if (disposeException != null)
        {
            ExceptionDispatchInfo.Capture(disposeException).Throw();
        }
    }

    /// <summary>
    ///     Disposes the specified objects
    ///     if they are <see cref="LuaReference"/>s.
    /// </summary>
    /// <param name="possibleReferences"> The possible references. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Dispose<T>(IEnumerable<T>? possibleReferences)
        where T : class?
    {
        if (possibleReferences == null)
        {
            return;
        }

        Exception? disposeException = null;
        foreach (var possibleReference in possibleReferences)
        {
            if (possibleReference is LuaReference reference)
            {
                try
                {
                    reference.Dispose();
                }
                catch (Exception ex)
                {
                    disposeException ??= ex;
                }
            }
        }

        if (disposeException != null)
        {
            ExceptionDispatchInfo.Capture(disposeException).Throw();
        }
    }

    /// <summary>
    ///     Disposes the specified keys and values
    ///     if they are <see cref="LuaReference"/>s.
    /// </summary>
    /// <param name="possibleReferences"> The possible references. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Dispose<TKey, TValue>(Dictionary<TKey, TValue>? possibleReferences)
        where TKey : notnull
    {
        if (possibleReferences == null)
        {
            return;
        }

        Exception? disposeException = null;
        foreach (var (possibleReferenceKey, possibleReferenceValue) in possibleReferences)
        {
            if (possibleReferenceKey is LuaReference referenceKey)
            {
                try
                {
                    referenceKey.Dispose();
                }
                catch (Exception ex)
                {
                    disposeException ??= ex;
                }
            }

            if (possibleReferenceValue is LuaReference referenceValue)
            {
                try
                {
                    referenceValue.Dispose();
                }
                catch (Exception ex)
                {
                    disposeException ??= ex;
                }
            }
        }

        if (disposeException != null)
        {
            ExceptionDispatchInfo.Capture(disposeException).Throw();
        }
    }

    /// <summary>
    ///     Disposes the specified keys and values
    ///     if they are <see cref="LuaReference"/>s.
    /// </summary>
    /// <param name="possibleReferences"> The possible references. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Dispose<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>>? possibleReferences)
    {
        if (possibleReferences == null)
        {
            return;
        }

        Exception? disposeException = null;
        foreach (var (possibleReferenceKey, possibleReferenceValue) in possibleReferences)
        {
            if (possibleReferenceKey is LuaReference referenceKey)
            {
                try
                {
                    referenceKey.Dispose();
                }
                catch (Exception ex)
                {
                    disposeException ??= ex;
                }
            }

            if (possibleReferenceValue is LuaReference referenceValue)
            {
                try
                {
                    referenceValue.Dispose();
                }
                catch (Exception ex)
                {
                    disposeException ??= ex;
                }
            }
        }

        if (disposeException != null)
        {
            ExceptionDispatchInfo.Capture(disposeException).Throw();
        }
    }
}
