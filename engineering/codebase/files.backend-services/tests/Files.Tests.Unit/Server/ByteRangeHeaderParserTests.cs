using AwesomeAssertions;
using WoW.Two.Sdk.Beta.Be.Files.Server.Delivery;
using WoW.Two.Sdk.Beta.Be.Files.Store;

namespace Files.Tests.Unit.Server;

public sealed class ByteRangeHeaderParserTests
{
    [Theory]
    [InlineData("bytes=0-9", 0L, 9L)]
    [InlineData("bytes=5-", 5L, 99L)]
    [InlineData("bytes=90-200", 90L, 99L)]
    [InlineData("bytes=-10", 90L, 99L)]
    [InlineData("bytes=-500", 0L, 99L)]
    public void TryParse_ShouldServeOneRange_WhenOneRangeIsAsked(string header, long from, long to)
    {
        ByteRangeHeaderParser.TryParse(header, 100, out var range).Should().BeTrue();
        range.Should().Be(new ByteRange(from, to));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("items=0-9")]
    [InlineData("bytes=0-9,20-29")]
    [InlineData("nonsense")]
    public void TryParse_ShouldServeTheWholeFile_WhenNoSingleByteRangeIsAsked(string? header)
    {
        ByteRangeHeaderParser.TryParse(header, 100, out var range).Should().BeTrue();
        range.Should().BeNull();
    }

    [Theory]
    [InlineData("bytes=100-")]
    [InlineData("bytes=150-160")]
    [InlineData("bytes=-0")]
    public void TryParse_ShouldBeUnsatisfiable_WhenTheRangeMissesTheFile(string header)
    {
        ByteRangeHeaderParser.TryParse(header, 100, out _).Should().BeFalse();
    }
}
