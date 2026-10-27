using AwesomeAssertions;
using WoW.Two.Sdk.Beta.Be.Files.Formats.Types;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace Files.Tests.Unit.Formats;

public sealed class FileContentValidatorTests
{
    private static readonly byte[] Jpeg = Convert.FromHexString("FFD8FFE000104A46494600010100000100010000");
    private static readonly byte[] Png = Convert.FromHexString("89504E470D0A1A0A0000000D49484452");
    private static readonly byte[] Zip = Convert.FromHexString("504B0304140006000800000021000000");
    private static readonly byte[] Rtf = "{\\rtf1\\ansi\\deff0 hello}"u8.ToArray();
    private static readonly byte[] Pdf = "%PDF-1.7\n%\u00e2\u00e3"u8.ToArray();

    private static readonly FileContentValidator Validator = FileContentValidator.Default;

    [Fact]
    public void Validate_ShouldRecognizeTheType_WhenTheBytesMatchTheName()
    {
        var file = Value(Validator.Validate("photo.PNG", "image/png", Png));

        file.Name.Should().Be("photo.PNG");
        file.Type.MimeType.Should().Be("image/png");
        file.Type.Kind.Should().Be(FileKind.Image);
    }

    [Fact]
    public void Validate_ShouldFailWithContentMismatch_WhenAJpegIsRenamedToPng()
    {
        Error(Validator.Validate("photo.png", "image/png", Jpeg)).Message.Should().Contain("does not match");
    }

    [Fact]
    public void Validate_ShouldFailWithContentMismatch_WhenAnRtfPosesAsAWordDocument()
    {
        Error(Validator.Validate("invoice.doc", null, Rtf)).Type.Should().Be(AppErrorType.Validation);
    }

    [Fact]
    public void Validate_ShouldAcceptTheContainer_WhenAnOfficeFileCarriesZipBytes()
    {
        Value(Validator.Validate("report.docx", null, Zip)).Type.Kind.Should().Be(FileKind.Document);
    }

    [Fact]
    public void Validate_ShouldFailWithContentMismatch_WhenATextNameHidesABinary()
    {
        Error(Validator.Validate("notes.txt", "text/plain", Pdf)).Type.Should().Be(AppErrorType.Validation);
        Error(Validator.Validate("notes.txt", "text/plain", Zip)).Type.Should().Be(AppErrorType.Validation);
    }

    [Fact]
    public void Validate_ShouldAcceptText_WhenItOnlyStartsLikeAShortSignature()
    {
        Value(Validator.Validate("cars.txt", null, "BMW and MZ notes"u8)).Type.MimeType.Should().Be("text/plain");
    }

    [Fact]
    public void Validate_ShouldStoreTheRegistryType_WhenTheUploaderDeclaredAnother()
    {
        Value(Validator.Validate("data.csv", "application/vnd.ms-excel", "a,b\n1,2"u8)).Type.MimeType.Should().Be("text/csv");
    }

    [Fact]
    public void Validate_ShouldDetectTheTypeAndAddItsExtension_WhenTheNameHasNone()
    {
        var file = Value(Validator.Validate("photo", null, Jpeg));

        file.Name.Should().Be("photo.jpg");
        file.Type.MimeType.Should().Be("image/jpeg");
    }

    [Fact]
    public void Validate_ShouldUseTheDeclaredType_WhenTheNameHasNoExtension()
    {
        Value(Validator.Validate("scan", "application/pdf", Pdf)).Name.Should().Be("scan.pdf");
        Error(Validator.Validate("scan", "application/pdf", Jpeg)).Type.Should().Be(AppErrorType.Validation);
    }

    [Fact]
    public void Validate_ShouldDetectFromBytes_WhenTheExtensionIsUnknown()
    {
        var file = Value(Validator.Validate("export.xyz", null, Zip));

        file.Name.Should().Be("export.xyz");
        file.Type.Extension.Should().Be("zip");
    }

    [Fact]
    public void Validate_ShouldKeepAnUnknownTypeGeneric_WhenNoSignatureMatches()
    {
        var file = Value(Validator.Validate("model.xyz", null, "plain words"u8));

        file.Type.Kind.Should().Be(FileKind.Generic);
        file.Type.MimeType.Should().Be("application/octet-stream");
        file.Type.Label.Should().Be("XYZ");
    }

    [Fact]
    public void Validate_ShouldAcceptAnyBytes_WhenTheExtensionIsGeneric()
    {
        Value(Validator.Validate("disk.bin", null, Jpeg)).Type.Kind.Should().Be(FileKind.Generic);
    }

    [Fact]
    public void Validate_ShouldPassAShortHead_WhenMoreBytesFollow()
    {
        Value(Validator.Validate("photo.png", null, Png.AsSpan(0, 3))).Type.Extension.Should().Be("png");
        Error(Validator.Validate("photo.png", null, Png.AsSpan(0, 3), complete: true)).Type.Should().Be(AppErrorType.Validation);
    }

    [Fact]
    public void Validate_ShouldFailWithNameInvalid_WhenTheNameIsBlank()
    {
        Error(Validator.Validate("  ", null, Png)).Message.Should().Contain("name");
    }

    private static RecognizedFileModel Value(Result<RecognizedFileModel> result) =>
        result is Result<RecognizedFileModel>.Success success
            ? success.Value
            : throw new Xunit.Sdk.XunitException(((Result<RecognizedFileModel>.Failure)result).Error.Message);

    private static AppError Error(Result<RecognizedFileModel> result) =>
        result is Result<RecognizedFileModel>.Failure failure
            ? failure.Error
            : throw new Xunit.Sdk.XunitException("The validation passed.");
}
