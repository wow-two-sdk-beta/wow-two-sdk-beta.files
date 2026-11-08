using AwesomeAssertions;
using WoW.Two.Sdk.Beta.Be.Files.Formats.Types;
using WoW.Two.Sdk.Beta.Be.Files.Module.Policies;
using WoW.Two.Sdk.Beta.Be.Files.Module.Variants;

namespace Files.Tests.Unit.Variants;

public sealed class FileVariantWidthMapperTests
{
    [Fact]
    public void Plan_ShouldKeepOnlyWidthsNoWiderThanTheSource_NarrowestFirst()
    {
        FileVariantWidthMapper.Plan(2000, [1400, 480, 1200, 900]).Should().Equal(480, 900, 1200, 1400);
        FileVariantWidthMapper.Plan(1000, [1400, 480, 1200, 900]).Should().Equal(480, 900);
    }

    [Fact]
    public void Plan_ShouldNeverUpscale_WhenTheSourceIsNarrowerThanEveryWidth()
    {
        FileVariantWidthMapper.Plan(300, [480, 900]).Should().BeEmpty();
    }

    [Fact]
    public void Plan_ShouldKeepAWidthEqualToTheSource_AndDropRepeatsAndNonPositiveWidths()
    {
        FileVariantWidthMapper.Plan(900, [900, 480, 480, 0, -5]).Should().Equal(480, 900);
    }

    [Theory]
    [InlineData(1, 480)]
    [InlineData(480, 480)]
    [InlineData(481, 900)]
    [InlineData(1000, 1200)]
    [InlineData(1400, 1400)]
    [InlineData(5000, 1400)]
    public void Select_ShouldPickTheNarrowestAtLeastAsWide_ElseTheWidest(int asked, int expected)
    {
        FileVariantWidthMapper.Select([1400, 480, 1200, 900], asked).Should().Be(expected);
    }

    [Fact]
    public void Select_ShouldPickNothing_WhenTheFileHasNoVariants()
    {
        FileVariantWidthMapper.Select([], 480).Should().BeNull();
    }

    [Theory]
    [InlineData("photo.jpg", true)]
    [InlineData("photo.png", true)]
    [InlineData("photo.webp", true)]
    [InlineData("anim.gif", true)]
    [InlineData("vector.svg", false)]
    [InlineData("photo.heic", false)]
    [InlineData("doc.pdf", false)]
    public void IsSource_ShouldAcceptRasterImagesOnly(string name, bool expected)
    {
        FileVariantWidthMapper.IsSource(FileTypeMapper.Default.FromName(name)!).Should().Be(expected);
    }

    [Fact]
    public void IsValid_ShouldRefuseWidthsOutOfRangeAndQualityOutOfRange()
    {
        var policy = new FilePolicyOptions { Variants = { 480, 1400 } };
        FileVariantWidthMapper.IsValid(policy).Should().BeTrue();
        new FilePolicyOptions().VariantQuality.Should().Be(80);

        FileVariantWidthMapper.IsValid(new FilePolicyOptions { Variants = { 0 } }).Should().BeFalse();
        FileVariantWidthMapper.IsValid(new FilePolicyOptions { Variants = { 9000 } }).Should().BeFalse();
        FileVariantWidthMapper.IsValid(new FilePolicyOptions { VariantQuality = 101 }).Should().BeFalse();
    }
}
