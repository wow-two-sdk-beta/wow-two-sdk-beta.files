using AwesomeAssertions;
using WoW.Two.Sdk.Beta.Be.Files.Formats;
using WoW.Two.Sdk.Beta.Be.Files.Formats.Types;
using WoW.Two.Sdk.Beta.Be.Files.Store;

namespace Files.Tests.Unit.Formats;

public sealed class FileTypeMapperTests
{
    private static readonly FileTypeMapper Types = FileTypeMapper.Default;

    [Fact]
    public void Default_ShouldHoldEveryKindOnce_WhenTheEmbeddedRegistryLoads()
    {
        Types.Types.Should().HaveCountGreaterThan(300);
        Types.Types.Select(type => type.Kind).Distinct().Should().BeEquivalentTo(Enum.GetValues<FileKind>());
        Types.Types.Should().OnlyContain(type =>
            type.Extension == type.Extension.ToLowerInvariant() && type.Label.Length > 0 && type.Label.Length <= 8 && type.MimeType.Contains('/'));
    }

    [Fact]
    public void Default_ShouldKeepEverySignatureWithinTheHead_WhenTheRegistryLoads()
    {
        Types.Types.SelectMany(type => type.Signatures).SelectMany(signature => signature.Parts)
            .Should().OnlyContain(part => part.Offset + part.Bytes.Length <= FileTypeConstants.HeadLength);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenTwoRowsShareAnExtension()
    {
        var row = FileTypeModel.Unknown("dup");

        var build = () => new FileTypeMapper([row, row with { MimeType = "text/plain" }]);

        build.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("PNG", "png")]
    [InlineData(".jpeg", "jpeg")]
    [InlineData("glb", "glb")]
    public void FromExtension_ShouldIgnoreCaseAndTheDot_WhenTheExtensionIsKnown(string extension, string expected)
    {
        Types.FromExtension(extension)!.Extension.Should().Be(expected);
    }

    [Fact]
    public void FromName_ShouldUseTheLastExtension_WhenTheNameHasSeveralDots()
    {
        Types.FromName("backup.2026.tar.gz")!.Extension.Should().Be("gz");
        Types.FromName("folder.v2/readme")!.Should().BeNull();
    }

    [Fact]
    public void FromMimeType_ShouldIgnoreParameters_WhenTheTypeIsKnown()
    {
        Types.FromMimeType("text/plain; charset=utf-8")!.Extension.Should().Be("txt");
        Types.FromMimeType("IMAGE/JPEG")!.Extension.Should().Be("jpg");
        Types.FromMimeType("application/x-unknown").Should().BeNull();
    }

    [Theory]
    [InlineData("FFD8FFE000104A464946", "jpg")]
    [InlineData("89504E470D0A1A0A0000", "png")]
    [InlineData("255044462D312E37", "pdf")]
    [InlineData("504B030414000600", "zip")]
    [InlineData("D0CF11E0A1B11AE10000", "doc")]
    [InlineData("4D5A90000300", "exe")]
    [InlineData("0000001C6674797068656963", "heic")]
    [InlineData("000000206674797069736F6D", "mp4")]
    [InlineData("0000001466747970717420200000", "mov")]
    [InlineData("524946462400000057415645", "wav")]
    [InlineData("524946462400000057454250", "webp")]
    public void FromContent_ShouldDetectTheMostSpecificType_WhenASignatureMatches(string hex, string expected)
    {
        Types.FromContent(Convert.FromHexString(hex))!.Extension.Should().Be(expected);
    }

    [Fact]
    public void FromContent_ShouldReachDeepSignatures_WhenTheHeadCoversThem()
    {
        var iso = new byte[FileTypeConstants.HeadLength];
        "CD001"u8.CopyTo(iso.AsSpan(32769));

        Types.FromContent(iso)!.Extension.Should().Be("iso");
    }

    [Fact]
    public void FromContent_ShouldDetectNothing_WhenTheBytesAreText()
    {
        Types.FromContent("BMW quarterly report"u8).Should().BeNull();
    }
}
