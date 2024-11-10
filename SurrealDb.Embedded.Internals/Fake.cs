using System.Runtime.InteropServices;

namespace SurrealDb.Embedded.Internals;

[StructLayout(LayoutKind.Sequential)]
internal unsafe partial struct ByteBuffer
{
    public byte* ptr;
    public int length;
    public int capacity;
}
