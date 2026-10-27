namespace WoW.Two.Sdk.Beta.Be.Files.Module.Store;

/// <summary>Reads a window of another stream — <c>length</c> bytes from <c>from</c> — and disposes it when done.</summary>
internal sealed class RangeReadStream : Stream
{
    private readonly Stream inner;
    private long remaining;

    private RangeReadStream(Stream inner, long length)
    {
        this.inner = inner;
        remaining = length;
        Length = length;
    }

    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length { get; }

    /// <inheritdoc />
    public override long Position
    {
        get => Length - remaining;
        set => throw new NotSupportedException();
    }

    /// <summary>Positions <paramref name="inner"/> at <paramref name="from"/> — by seeking, or by reading past the bytes
    /// before it — and wraps it.</summary>
    public static async Task<Stream> OpenAsync(Stream inner, long from, long length, CancellationToken cancellationToken)
    {
        if (inner.CanSeek)
        {
            inner.Seek(from, SeekOrigin.Begin);
        }
        else
        {
            var buffer = new byte[81920];
            for (var skipped = 0L; skipped < from;)
            {
                var read = await inner.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, from - skipped)), cancellationToken);
                if (read == 0)
                    break;
                skipped += read;
            }
        }

        return new RangeReadStream(inner, length);
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        if (remaining <= 0)
            return 0;
        var read = inner.Read(buffer[..(int)Math.Min(buffer.Length, remaining)]);
        remaining -= read;
        return read;
    }

    /// <inheritdoc />
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (remaining <= 0)
            return 0;
        var read = await inner.ReadAsync(buffer[..(int)Math.Min(buffer.Length, remaining)], cancellationToken);
        remaining -= read;
        return read;
    }

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void Flush() { }

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            inner.Dispose();
        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await inner.DisposeAsync();
        await base.DisposeAsync();
    }
}
