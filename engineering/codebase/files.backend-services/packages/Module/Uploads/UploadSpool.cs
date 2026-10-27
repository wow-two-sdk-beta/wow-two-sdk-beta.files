using System.Security.Cryptography;
using WoW.Two.Sdk.Beta.Be.Files.Formats;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Uploads;

/// <summary>Holds an upload's bytes in a temporary file while they are checked: their SHA-256, size and leading
/// bytes are known before anything is stored. Disposing deletes the file.</summary>
internal sealed class UploadSpool : IAsyncDisposable
{
    private readonly FileStream file;

    private UploadSpool(FileStream file, string sha256, long size, byte[] head)
    {
        this.file = file;
        Sha256 = sha256;
        Size = size;
        Head = head;
    }

    /// <summary>Gets the SHA-256 of the bytes, lowercase hex.</summary>
    public string Sha256 { get; }

    /// <summary>Gets the size in bytes.</summary>
    public long Size { get; }

    /// <summary>Gets up to <see cref="FileTypeConstants.HeadLength"/> leading bytes.</summary>
    public byte[] Head { get; }

    /// <summary>Gets whether <see cref="Head"/> holds the whole file.</summary>
    public bool HeadIsComplete => Head.LongLength == Size;

    /// <summary>Reads <paramref name="content"/> to its end into a temporary file, hashing it on the way.</summary>
    /// <param name="content">The upload's bytes.</param>
    /// <param name="maxBytes">The policy's size limit; a longer upload stops early.</param>
    /// <param name="folder">The folder the temporary file goes in.</param>
    /// <param name="cancellationToken">The token that cancels the read.</param>
    /// <returns>The spool; <see cref="FilesErrors.FileTooLarge"/> past the limit.</returns>
    public static async Task<Result<UploadSpool>> CreateAsync(Stream content, long maxBytes, string folder, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(folder);
        var file = new FileStream(
            Path.Combine(folder, $"files-upload-{Guid.NewGuid():N}.part"),
            FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920,
            FileOptions.Asynchronous | FileOptions.DeleteOnClose);

        try
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var head = new MemoryStream();
            var buffer = new byte[81920];
            long size = 0;
            int read;
            while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
            {
                size += read;
                if (size > maxBytes)
                {
                    await file.DisposeAsync();
                    return Result<UploadSpool>.Fail(FilesErrors.FileTooLarge(maxBytes));
                }

                hash.AppendData(buffer, 0, read);
                if (head.Length < FileTypeConstants.HeadLength)
                    head.Write(buffer, 0, (int)Math.Min(read, FileTypeConstants.HeadLength - head.Length));
                await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            await file.FlushAsync(cancellationToken);
            var sha256 = Convert.ToHexStringLower(hash.GetHashAndReset());
            return Result<UploadSpool>.Ok(new UploadSpool(file, sha256, size, head.ToArray()));
        }
        catch
        {
            await file.DisposeAsync();
            throw;
        }
    }

    /// <summary>Opens the spooled bytes from their start; the stream stays owned by the spool.</summary>
    public Stream OpenRead()
    {
        file.Position = 0;
        return new NonClosingStream(file);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => file.DisposeAsync();

    /// <summary>Lends the spool's file to a reader that disposes what it is given.</summary>
    private sealed class NonClosingStream(Stream inner) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => inner.Length;

        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            inner.ReadAsync(buffer, cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void Flush() { }
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
