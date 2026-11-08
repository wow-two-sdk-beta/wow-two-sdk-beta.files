using System.Buffers;
using System.Security.Cryptography;
using Files.Api.Migrate.Sources;

namespace Files.Api.Migrate.Api;

/// <summary>Holds one item's bytes and their SHA-256, known before the upload starts and replayable for every retry: a
/// row up to the memory limit stays in memory, a larger one spills to a temporary file, and a source file is reopened
/// in place.</summary>
public sealed class ContentBuffer : IAsyncDisposable
{
    private const int ChunkSize = 81920;

    private readonly MemoryStream? memory;
    private readonly string? file;
    private readonly bool ownsFile;

    private ContentBuffer(MemoryStream? memory, string? file, bool ownsFile, string sha256, long length)
    {
        this.memory = memory;
        this.file = file;
        this.ownsFile = ownsFile;
        Sha256 = sha256;
        Length = length;
    }

    /// <summary>Gets the lowercase hex SHA-256 of the bytes.</summary>
    public string Sha256 { get; }

    /// <summary>Gets the number of bytes.</summary>
    public long Length { get; }

    /// <summary>Gets whether the bytes spilled to a temporary file.</summary>
    public bool IsSpilled => ownsFile;

    /// <summary>Reads an item's bytes once, hashing them on the way.</summary>
    /// <param name="item">The source item.</param>
    /// <param name="memoryLimit">The most bytes held in memory before the rest spills to a file.</param>
    /// <param name="tempDirectory">The folder a spilled item's file is written to.</param>
    /// <param name="cancellationToken">The token that cancels the read.</param>
    public static async Task<ContentBuffer> FillAsync(SourceItem item, long memoryLimit, string tempDirectory, CancellationToken cancellationToken)
    {
        if (item.FilePath is { } path)
        {
            await using var source = item.OpenContent();
            var (hash, length) = await HashAsync(source, cancellationToken);
            return new ContentBuffer(null, path, ownsFile: false, hash, length);
        }

        await using var content = item.OpenContent();
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var chunk = ArrayPool<byte>.Shared.Rent(ChunkSize);
        MemoryStream? memory = new();
        FileStream? spill = null;
        string? spillPath = null;
        long total = 0;
        try
        {
            int read;
            while ((read = await content.ReadAsync(chunk.AsMemory(0, ChunkSize), cancellationToken)) > 0)
            {
                sha.AppendData(chunk, 0, read);
                total += read;
                if (spill is null && memory!.Length + read > memoryLimit)
                {
                    spillPath = Path.Combine(tempDirectory, $"files-migrate-{Guid.NewGuid():N}.part");
                    spill = new FileStream(spillPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, ChunkSize, FileOptions.Asynchronous);
                    memory.Position = 0;
                    await memory.CopyToAsync(spill, cancellationToken);
                    await memory.DisposeAsync();
                    memory = null;
                }

                if (spill is not null)
                    await spill.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
                else
                    memory!.Write(chunk, 0, read);
            }

            var hash = Convert.ToHexStringLower(sha.GetHashAndReset());
            if (spill is null)
                return new ContentBuffer(memory, null, ownsFile: false, hash, total);

            await spill.DisposeAsync();
            spill = null;
            return new ContentBuffer(null, spillPath, ownsFile: true, hash, total);
        }
        catch
        {
            if (spill is not null)
                await spill.DisposeAsync();
            Delete(spillPath);
            throw;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(chunk);
        }
    }

    /// <summary>Hashes a stream to its end.</summary>
    /// <param name="stream">The bytes to hash.</param>
    /// <param name="cancellationToken">The token that cancels the read.</param>
    /// <returns>The lowercase hex SHA-256 and the number of bytes read.</returns>
    public static async Task<(string Sha256, long Length)> HashAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var chunk = ArrayPool<byte>.Shared.Rent(ChunkSize);
        long total = 0;
        try
        {
            int read;
            while ((read = await stream.ReadAsync(chunk.AsMemory(0, ChunkSize), cancellationToken)) > 0)
            {
                sha.AppendData(chunk, 0, read);
                total += read;
            }

            return (Convert.ToHexStringLower(sha.GetHashAndReset()), total);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(chunk);
        }
    }

    /// <summary>Creates a fresh body for one upload attempt; the request disposes it, never the buffer.</summary>
    public HttpContent CreateContent() => memory is not null
        ? new ByteArrayContent(memory.GetBuffer(), 0, (int)memory.Length)
        : new StreamContent(new FileStream(file!, FileMode.Open, FileAccess.Read, FileShare.Read, ChunkSize, FileOptions.Asynchronous | FileOptions.SequentialScan), ChunkSize);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (memory is not null)
            await memory.DisposeAsync();
        if (ownsFile)
            Delete(file);
    }

    private static void Delete(string? path)
    {
        if (path is null)
            return;
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // A temporary file left behind is swept by the host's temp cleanup.
        }
    }
}
