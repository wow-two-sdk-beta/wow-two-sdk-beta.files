using AwesomeAssertions;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Uploads;
using WoW.Two.Sdk.Beta.Be.Files.Urls;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using Xunit;

namespace WoW.Two.Sdk.Beta.Be.Files.Testing.Files;

/// <summary>The conformance suite of image variants: what an upload under an app with variant widths gets, and which
/// variant a URL serves.</summary>
/// <remarks>Derive a test class per implementation whose instance gives the app <see cref="App"/> a policy with the
/// variant widths <see cref="Widths"/> (<c>Files:Policies:conformance-variants:Variants</c> = 480, 900). The cases
/// upload real images, so the implementation's image service must decode PNG and encode WebP.</remarks>
public abstract class FileVariantsConformance
{
    /// <summary>Holds the app whose policy the instance under test gives the variant widths <see cref="Widths"/>.</summary>
    public const string App = "conformance-variants";

    /// <summary>Gets the variant widths the instance under test gives <see cref="App"/>.</summary>
    public static IReadOnlyList<int> Widths { get; } = [480, 900];

    /// <summary>Gets the brokers as the tenant under test reaches them.</summary>
    protected abstract FilesBrokerSet Brokers { get; }

    /// <summary>An image wider than every width gets one WebP per width, keeping its aspect ratio.</summary>
    [Fact]
    public async Task UploadAsync_ShouldMakeAWebpPerWidth_WhenTheImageIsWiderThanEveryWidth()
    {
        var file = Value(await UploadAsync(FilesSamples.PngImage(1200, 800), "wide.png"));

        file.Variants.Select(variant => (variant.Width, variant.Height)).Should().Equal((480, 320), (900, 600));
        file.Variants.Should().OnlyContain(variant => variant.ContentType == "image/webp" && variant.Size > 0);
        Value(await Brokers.Store.GetAsync(file.Id)).Variants.Should().BeEquivalentTo(file.Variants);
    }

    /// <summary>A width wider than the image is skipped, never upscaled.</summary>
    [Fact]
    public async Task UploadAsync_ShouldSkipAWidth_WhenTheImageIsNarrowerThanIt()
    {
        var file = Value(await UploadAsync(FilesSamples.PngImage(600, 300), "narrow.png"));

        file.Variants.Select(variant => (variant.Width, variant.Height)).Should().Equal((480, 240));
    }

    /// <summary>A width picks the narrowest variant at least that wide, else the widest; signed URLs name it, public
    /// URLs point at its own bytes.</summary>
    [Fact]
    public async Task Urls_ShouldPickTheNarrowestVariantAtLeastAsWide_ElseTheWidest()
    {
        var file = Value(await UploadAsync(FilesSamples.PngImage(1200, 800), "pick.png"));
        Value(await Brokers.Store.UpdateAsync(file.Id, new FilePatch { Visibility = FileVisibility.Public }));

        (await SignedWidthAsync(file.Id, 100)).Should().Be("480");
        (await SignedWidthAsync(file.Id, 480)).Should().Be("480");
        (await SignedWidthAsync(file.Id, 481)).Should().Be("900");
        (await SignedWidthAsync(file.Id, 4000)).Should().Be("900");
        (await SignedWidthAsync(file.Id, null)).Should().BeNull();

        var variant = Value(await Brokers.Urls.GetPublicUrlAsync(file.Id, 600)).AbsolutePath;
        variant.Should().EndWith(".webp").And.NotContain(file.Sha256);
        Value(await Brokers.Urls.GetPublicUrlAsync(file.Id, 900)).AbsolutePath.Should().Be(variant);
        Value(await Brokers.Urls.GetPublicUrlAsync(file.Id)).AbsolutePath.Should().EndWith($"/m/{file.Sha256}.png");
    }

    /// <summary>Variants go with their source: a deleted file serves none until it is restored.</summary>
    [Fact]
    public async Task Urls_ShouldServeNoVariant_WhileTheSourceIsDeleted()
    {
        var file = Value(await UploadAsync(FilesSamples.PngImage(1000, 500), "gone.png"));

        Value(await Brokers.Store.DeleteAsync(file.Id));
        Error(await Brokers.Urls.IssueAsync(file.Id, new UrlSpec { Width = 480 })).Type.Should().Be(AppErrorType.FileNotFound);

        Value(await Brokers.Store.RestoreAsync(file.Id)).Variants.Select(restored => restored.Width).Should().Equal(Widths);
        (await SignedWidthAsync(file.Id, 480)).Should().Be("480");
    }

    private async Task<string?> SignedWidthAsync(FileId id, int? width)
    {
        var url = Value(await Brokers.Urls.IssueAsync(id, new UrlSpec { Width = width })).Url;
        return url.Query.TrimStart('?').Split('&')
            .Select(part => part.Split('=', 2))
            .FirstOrDefault(pair => pair[0] == "w")?[1];
    }

    private async Task<Result<FileModel>> UploadAsync(byte[] bytes, string name)
    {
        using var content = new MemoryStream(bytes);
        return await Brokers.Uploads.UploadAsync(content, new UploadSpec { Name = name, AppId = App });
    }

    /// <summary>Returns the success value, failing the test with the broker's own error otherwise.</summary>
    private static T Value<T>(Result<T> result) where T : notnull =>
        result is Result<T>.Success success
            ? success.Value
            : throw new Xunit.Sdk.XunitException(((Result<T>.Failure)result).Error.Message);

    /// <summary>Returns the failure's error, failing the test when the call succeeded.</summary>
    private static AppError Error<T>(Result<T> result) where T : notnull =>
        result is Result<T>.Failure failure ? failure.Error : throw new Xunit.Sdk.XunitException("The call succeeded.");
}
