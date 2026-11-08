using System.Web;
using AwesomeAssertions;
using Files.Tests.Integration.Files;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using WoW.Two.Sdk.Beta.Be.Files.Module.Delivery;
using WoW.Two.Sdk.Beta.Be.Files.Module.Sweeps;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Uploads;
using WoW.Two.Sdk.Beta.Be.Files.Urls;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using static Files.Tests.Integration.Files.ModuleHarness;

namespace Files.Tests.Integration.Variants;

/// <summary>Image variants through the module: one WebP per policy width on upload, never upscaled, served by URL like
/// the original, and gone with their source.</summary>
public sealed class FileVariantsTests : IAsyncLifetime
{
    private ModuleHarness harness = null!;

    public async Task InitializeAsync() => harness = await CreateAsync(options =>
    {
        options.Policies["shop"] = new() { Variants = { 900, 480 } };
        options.Policies["ocharo"] = new() { Variants = { 480, 900, 1200, 1400 }, VariantQuality = 70 };
        options.Delivery.BaseUrl = new Uri("https://files.example.test");
        options.Delivery.SigningKey = "integration-signing-key-0123456789abcdef";
    });

    public async Task DisposeAsync() => await harness.DisposeAsync();

    [Fact]
    public async Task UploadAsync_ShouldMakeTwoWebpVariants_WhenA2000PxJpegIsUploadedUnderWidths480And900()
    {
        var file = Value(await harness.UploadAsync(Jpeg(2000, 1500), "photo.jpg", appId: "shop"));

        file.ContentType.Should().Be("image/jpeg");
        file.Variants.Select(variant => (variant.Width, variant.Height)).Should().Equal((480, 360), (900, 675));
        file.Variants.Should().OnlyContain(variant => variant.ContentType == "image/webp" && variant.Size > 0);

        var rows = await harness.RowsAsync("SELECT width, height, content_type, size, sha256 FROM files_variants ORDER BY width");
        rows.Select(row => (long)row["width"]!).Should().Equal(480L, 900L);
        rows.Should().OnlyContain(row => (string)row["content_type"]! == "image/webp" && ((string)row["sha256"]!).Length == 64);
        harness.BlobFiles().Should().HaveCount(3);
        Value(await harness.StoreAsync(store => store.GetAsync(file.Id))).Variants.Should().BeEquivalentTo(file.Variants);
    }

    [Fact]
    public async Task UploadAsync_ShouldMakeOnlyThe480Variant_WhenTheSourceIs600PxWide()
    {
        var file = Value(await harness.UploadAsync(Jpeg(600, 400), "small.jpg", appId: "shop"));

        file.Variants.Select(variant => (variant.Width, variant.Height)).Should().Equal((480, 320));
    }

    [Fact]
    public async Task UploadAsync_ShouldMakeEveryOcharoWidth_WhenTheSourceIsWiderThanAll()
    {
        var file = Value(await harness.UploadAsync(Png(1600, 800), "hero.png", appId: "ocharo"));

        file.Variants.Select(variant => variant.Width).Should().Equal(480, 900, 1200, 1400);
        file.Variants.Select(variant => variant.Height).Should().Equal(240, 450, 600, 700);
    }

    [Fact]
    public async Task UploadAsync_ShouldMakeNoVariant_WhenTheFileIsNotAnImage()
    {
        var file = Value(await harness.UploadAsync(Pdf, "report.pdf", appId: "shop"));

        file.Variants.Should().BeEmpty();
        (await harness.RowsAsync("SELECT * FROM files_variants")).Should().BeEmpty();
    }

    [Fact]
    public async Task UploadAsync_ShouldMakeNoVariant_WhenThePolicyNamesNoWidth()
    {
        Value(await harness.UploadAsync(Jpeg(2000, 1000), "photo.jpg")).Variants.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadAsync_ShouldStoreTheFileWithoutVariants_WhenTheImageIsBroken()
    {
        var broken = ModuleHarness.Jpeg.Concat(new byte[512]).ToArray();

        var file = Value(await harness.UploadAsync(broken, "broken.jpg", appId: "shop"));

        file.State.Should().Be(FileState.Ready);
        file.Size.Should().Be(broken.Length);
        file.Variants.Should().BeEmpty();
        (await harness.RowsAsync("SELECT * FROM files_variants")).Should().BeEmpty();
        harness.BlobFiles().Should().ContainSingle();
    }

    [Fact]
    public async Task UploadAsync_ShouldReturnTheFirstUploadsVariants_WhenRetriedWithTheSameKey()
    {
        var bytes = Jpeg(1000, 500);
        var first = Value(await harness.UploadAsync(bytes, "a.jpg", appId: "shop", idempotencyKey: "retry-1"));
        var retry = Value(await harness.UploadAsync(bytes, "a.jpg", appId: "shop", idempotencyKey: "retry-1"));

        retry.Id.Should().Be(first.Id);
        retry.Variants.Should().BeEquivalentTo(first.Variants);
        (await harness.RowsAsync("SELECT * FROM files_variants")).Should().HaveCount(2);
    }

    [Fact]
    public async Task UploadAsync_ShouldStoreAVariantsBytesOncePerTenant_WhenTwoFilesShareTheImage()
    {
        var bytes = Jpeg(1000, 500);
        Value(await harness.UploadAsync(bytes, "a.jpg", appId: "shop"));
        Value(await harness.UploadAsync(bytes, "b.jpg", appId: "shop"));

        (await harness.RowsAsync("SELECT * FROM files_variants")).Should().HaveCount(4);
        (await harness.RowsAsync("SELECT * FROM files_blobs")).Should().HaveCount(3);
        harness.BlobFiles().Should().HaveCount(3);
    }

    [Fact]
    public async Task PublicUrl_ShouldServeTheVariantsWebpBytes_AtTheImmutableRoute()
    {
        var file = await UploadPublicAsync(Jpeg(2000, 1500));

        var url = Value(await harness.WithAsync<IFileUrlsBroker, Result<Uri>>(urls => urls.GetPublicUrlAsync(file.Id, 500)));
        url.AbsolutePath.Should().EndWith(".webp").And.StartWith("/t/t1/m/");
        var sha256 = url.Segments[^1].Split('.')[0];

        var found = Value(await harness.WithAsync<IFilesDeliveryService, Result<DeliverableFileModel>>(delivery => delivery.FindPublicAsync("t1", sha256)));
        found.Should().Match<DeliverableFileModel>(served =>
            served.ContentType == "image/webp" && served.Sha256 == sha256 && served.VariantWidth == 900
            && served.Name == "photo-900.webp" && served.Size == file.Variants[1].Size);
        var bytes = await OpenAsync(found);
        IsWebp(bytes).Should().BeTrue();
        Sha256(bytes).Should().Be(sha256);
        Decode(bytes).Should().Be((900, 675));
    }

    [Fact]
    public async Task SignedUrl_ShouldServeTheVariantItNames_AndRefuseAnotherWidth()
    {
        var file = Value(await harness.UploadAsync(Jpeg(2000, 1500), "private.jpg", appId: "shop"));

        var signed = Value(await harness.WithAsync<IFileUrlsBroker, Result<SignedFileUrl>>(urls => urls.IssueAsync(file.Id, new UrlSpec { Width = 300 })));
        var query = HttpUtility.ParseQueryString(signed.Url.Query);
        query["w"].Should().Be("480");

        var found = Value(await FindSignedAsync(file.Id, query, 480));
        found.ContentType.Should().Be("image/webp");
        Decode(await OpenAsync(found)).Should().Be((480, 360));
        Error(await FindSignedAsync(file.Id, query, 900)).Message.Should().Contain("invalid");
        Error(await FindSignedAsync(file.Id, query, null)).Message.Should().Contain("invalid");
    }

    [Fact]
    public async Task Variants_ShouldFollowTheirSource_ThroughDeleteRestoreAndPurge()
    {
        var file = await UploadPublicAsync(Jpeg(1000, 500));
        var url = Value(await harness.WithAsync<IFileUrlsBroker, Result<Uri>>(urls => urls.GetPublicUrlAsync(file.Id, 480)));
        var sha256 = url.Segments[^1].Split('.')[0];

        Value(await harness.StoreAsync(store => store.DeleteAsync(file.Id)));
        Error(await FindPublicAsync(sha256)).Message.Should().Contain("not found");

        Value(await harness.StoreAsync(store => store.RestoreAsync(file.Id))).Variants.Should().HaveCount(2);
        Value(await FindPublicAsync(sha256)).VariantWidth.Should().Be(480);

        Value(await harness.StoreAsync(store => store.DeleteAsync(file.Id)));
        harness.Clock.Advance(TimeSpan.FromDays(31));
        await harness.WithAsync<IFilesSweepService, FileSweepResultModel>(sweep => sweep.SweepAsync());

        (await harness.RowsAsync("SELECT * FROM files_variants")).Should().BeEmpty();
        (await harness.RowsAsync("SELECT * FROM files_blobs")).Should().BeEmpty();
        harness.BlobFiles().Should().BeEmpty();
    }

    [Fact]
    public async Task Sweep_ShouldKeepAVariantsBytes_WhileItsSourceLives()
    {
        Value(await harness.UploadAsync(Jpeg(1000, 500), "kept.jpg", appId: "shop"));

        harness.Clock.Advance(TimeSpan.FromHours(2));
        await harness.WithAsync<IFilesSweepService, FileSweepResultModel>(sweep => sweep.SweepAsync());

        harness.BlobFiles().Should().HaveCount(3);
    }

    private async Task<FileModel> UploadPublicAsync(byte[] bytes)
    {
        await using var scope = harness.As("t1", appId: "shop");
        using var content = new MemoryStream(bytes);
        return Value(await scope.ServiceProvider.GetRequiredService<IFileUploadsBroker>()
            .UploadAsync(content, new UploadSpec { Name = "photo.jpg", Visibility = FileVisibility.Public }));
    }

    private Task<Result<DeliverableFileModel>> FindPublicAsync(string sha256) =>
        harness.WithAsync<IFilesDeliveryService, Result<DeliverableFileModel>>(delivery => delivery.FindPublicAsync("t1", sha256));

    private Task<Result<DeliverableFileModel>> FindSignedAsync(FileId id, System.Collections.Specialized.NameValueCollection query, int? width) =>
        harness.WithAsync<IFilesDeliveryService, Result<DeliverableFileModel>>(delivery =>
            delivery.FindSignedAsync("t1", id, long.Parse(query["exp"]!), query["d"], query["sig"], width));

    private async Task<byte[]> OpenAsync(DeliverableFileModel file)
    {
        var opened = Value(await harness.WithAsync<IFilesDeliveryService, Result<FileContent>>(delivery => delivery.OpenAsync(file, null)));
        await using var stream = opened.Content;
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy);
        opened.ContentType.Should().Be(file.ContentType);
        return copy.ToArray();
    }

    private static bool IsWebp(byte[] bytes) =>
        bytes.Length > 12 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8);

    private static (int Width, int Height) Decode(byte[] bytes)
    {
        using var bitmap = SKBitmap.Decode(bytes);
        return (bitmap.Width, bitmap.Height);
    }

    /// <summary>Encodes a gradient of the given size as a JPEG.</summary>
    private static byte[] Jpeg(int width, int height) => Encode(width, height, SKEncodedImageFormat.Jpeg);

    /// <summary>Encodes a gradient of the given size as a PNG.</summary>
    private static byte[] Png(int width, int height) => Encode(width, height, SKEncodedImageFormat.Png);

    private static byte[] Encode(int width, int height, SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        using (var paint = new SKPaint())
        {
            paint.Shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(width, height),
                [SKColors.OrangeRed, SKColors.SteelBlue], SKShaderTileMode.Clamp);
            canvas.DrawRect(0, 0, width, height, paint);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 90);
        return data.ToArray();
    }
}
