using System;
using System.Runtime.InteropServices;

namespace Laylua.Moon;

internal sealed class Utf8StringMarshaler : ICustomMarshaler
{
    private static readonly Utf8StringMarshaler Instance = new();

    public static ICustomMarshaler GetInstance(string cookie)
    {
        return Instance;
    }

    public object MarshalNativeToManaged(IntPtr pNativeData)
    {
        return Marshal.PtrToStringUTF8(pNativeData)!;
    }

    public IntPtr MarshalManagedToNative(object ManagedObj)
    {
        return IntPtr.Zero;
    }

    public void CleanUpNativeData(IntPtr pNativeData)
    { }

    public void CleanUpManagedData(object ManagedObj)
    { }

    public int GetNativeDataSize()
    {
        return IntPtr.Size;
    }
}