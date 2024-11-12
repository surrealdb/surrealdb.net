﻿namespace SurrealDb.Embedded.RocksDb.Internals;

internal partial struct ByteBuffer
{
    public readonly unsafe ReadOnlySpan<byte> AsReadOnly()
    {
        return new ReadOnlySpan<byte>(ptr, length);
    }
}
