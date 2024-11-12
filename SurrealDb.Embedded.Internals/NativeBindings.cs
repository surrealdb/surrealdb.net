using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SurrealDb.Embedded.Internals;

internal static class NativeBindings
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static void DropGcHandle(nint ptr)
    {
        GCHandle.FromIntPtr(ptr).Free();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void SuccessCallback(nint ptr, ByteBuffer* value)
    {
        (GCHandle.FromIntPtr(ptr).Target as Action<ByteBuffer>)!.Invoke(*value);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void FailureCallback(nint ptr, ByteBuffer* value)
    {
        (GCHandle.FromIntPtr(ptr).Target as Action<ByteBuffer>)!.Invoke(*value);
    }
}
