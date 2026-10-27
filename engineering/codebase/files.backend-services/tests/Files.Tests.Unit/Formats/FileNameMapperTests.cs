using AwesomeAssertions;
using WoW.Two.Sdk.Beta.Be.Files;
using WoW.Two.Sdk.Beta.Be.Files.Formats.Types;
using WoW.Two.Sdk.Beta.Be.Files.Urls;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace Files.Tests.Unit.Formats;

public sealed class FileNameMapperTests
{
    [Theory]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData("C:\\Users\\ann\\photo.jpg", "photo.jpg")]
    [InlineData("a<b>c:d\"e|f?g*h.txt", "abcdefgh.txt")]
    [InlineData("tab\tand\nnewline.txt", "tab and newline.txt")]
    [InlineData("  many    spaces  .txt", "many spaces .txt")]
    [InlineData("trailing dots...", "trailing dots")]
    [InlineData("CON.txt", "_CON.txt")]
    [InlineData("lpt1", "_lpt1")]
    [InlineData(".env", ".env")]
    public void ToSafeName_ShouldStripWhatNoStoredNameKeeps_WhenTheNameIsUnsafe(string name, string expected)
    {
        Value(FileNameMapper.ToSafeName(name)).Should().Be(expected);
    }

    [Fact]
    public void ToSafeName_ShouldDropDirectionMarks_WhenANameDisguisesItsExtension()
    {
        Value(FileNameMapper.ToSafeName("invoice\u202Egpj.exe")).Should().Be("invoicegpj.exe");
    }

    [Fact]
    public void ToSafeName_ShouldComposeAccents_WhenTheNameIsDecomposed()
    {
        Value(FileNameMapper.ToSafeName("cafe\u0301.txt")).Should().Be("caf\u00e9.txt");
    }

    [Fact]
    public void ToSafeName_ShouldKeepTheExtension_WhenTheNameIsTooLong()
    {
        var safe = Value(FileNameMapper.ToSafeName(new string('a', 400) + ".pdf"));

        safe.Should().HaveLength(FilesConstants.MaxNameLength).And.EndWith(".pdf");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("../")]
    [InlineData("...")]
    [InlineData("\u202E\u200B")]
    public void ToSafeName_ShouldFailWithNameInvalid_WhenNothingUsableIsLeft(string? name)
    {
        FileNameMapper.ToSafeName(name).IsSuccess.Should().BeFalse();
    }

    [Theory]
    [InlineData("photo.JPG", "jpg")]
    [InlineData("archive.tar.gz", "gz")]
    [InlineData("dir.d/README", "")]
    [InlineData("ends.with.dot.", "")]
    [InlineData(null, "")]
    public void ExtensionOf_ShouldReturnTheLowercaseLastExtension_WhenThereIsOne(string? name, string expected)
    {
        FileNameMapper.ExtensionOf(name).Should().Be(expected);
    }

    [Fact]
    public void ToContentDisposition_ShouldCarryAnAsciiFallbackAndTheUtf8Name_WhenTheNameIsNotAscii()
    {
        FileNameMapper.ToContentDisposition("отчёт \"Q3\".pdf", FileDisposition.Attachment).Should().Be(
            "attachment; filename=\"_____ _Q3_.pdf\"; filename*=UTF-8''%D0%BE%D1%82%D1%87%D1%91%D1%82%20%22Q3%22.pdf");
        FileNameMapper.ToContentDisposition("a.png", FileDisposition.Inline).Should().StartWith("inline; ");
    }

    private static string Value(Result<string> result) =>
        result is Result<string>.Success success
            ? success.Value
            : throw new Xunit.Sdk.XunitException(((Result<string>.Failure)result).Error.Message);
}
