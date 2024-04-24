// <auto-generated>
// This code is generated by csbindgen.
// DON'T CHANGE THIS DIRECTLY.
// </auto-generated>
#pragma warning disable CS8500
#pragma warning disable CS8981
using System;
using System.Runtime.InteropServices;


namespace SurrealDb.Embedded.InMemory.Internals
{
    internal static unsafe partial class NativeMethods
    {
        const string __DllName = "surreal-memory";



        [DllImport(__DllName, EntryPoint = "execute", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void execute(int id, Method method, byte* bytes, int len, SuccessAction success, FailureAction failure);

        [DllImport(__DllName, EntryPoint = "free_u8_buffer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void free_u8_buffer(ByteBuffer* buffer);

        [DllImport(__DllName, EntryPoint = "dispose", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void dispose(int id);

        [DllImport(__DllName, EntryPoint = "create_global_runtime", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void create_global_runtime();


    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe partial struct ByteBuffer
    {
        public byte* ptr;
        public int length;
        public int capacity;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe partial struct RustGCHandle
    {
        public nint ptr;
        public delegate* unmanaged[Cdecl]<nint, void> drop_callback;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe partial struct SuccessAction
    {
        public RustGCHandle handle;
        public delegate* unmanaged[Cdecl]<nint, ByteBuffer*, void> callback;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe partial struct FailureAction
    {
        public RustGCHandle handle;
        public delegate* unmanaged[Cdecl]<nint, ByteBuffer*, void> callback;
    }


    internal enum Method : byte
    {
        Connect = 1,
        Ping = 2,
        Use = 3,
        Set = 4,
        Unset = 5,
        Select = 6,
        Create = 8,
        Update = 9,
        Merge = 10,
        Patch = 11,
        Delete = 12,
        Version = 13,
        Query = 14,
    }


}
