using System.Net;
using System.Text;
using AwesomeAssertions;
using WoW.Two.Sdk.Beta.Be.Files.Client.Transport;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;

namespace Files.Tests.Unit.Client;

public sealed class FilesApiTransportTests
{
    [Theory]
    [InlineData("FileNotFound", HttpStatusCode.NotFound, AppErrorType.FileNotFound)]
    [InlineData("Validation", HttpStatusCode.BadRequest, AppErrorType.Validation)]
    [InlineData("Conflict", HttpStatusCode.Conflict, AppErrorType.Conflict)]
    public async Task ReadErrorAsync_ShouldRestoreTheServicesErrorType_WhenTheProblemCarriesItsCode(string code, HttpStatusCode status, AppErrorType expected)
    {
        using var response = Problem(status, $$"""{"title":"t","detail":"The detail.","code":"{{code}}"}""");

        var error = await FilesApiTransport.ReadErrorAsync(response, CancellationToken.None);

        error.Type.Should().Be(expected);
        error.Message.Should().Be("The detail.");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, AppErrorType.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, AppErrorType.Forbidden)]
    [InlineData(HttpStatusCode.NotFound, AppErrorType.NotFound)]
    [InlineData(HttpStatusCode.RequestedRangeNotSatisfiable, AppErrorType.Validation)]
    [InlineData(HttpStatusCode.BadGateway, AppErrorType.ExternalUnavailable)]
    public async Task ReadErrorAsync_ShouldFallBackToTheStatus_WhenTheBodyIsNoProblem(HttpStatusCode status, AppErrorType expected)
    {
        using var response = new HttpResponseMessage(status) { Content = new StringContent("<html>gateway</html>", Encoding.UTF8, "text/html") };

        (await FilesApiTransport.ReadErrorAsync(response, CancellationToken.None)).Type.Should().Be(expected);
    }

    private static HttpResponseMessage Problem(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/problem+json") };
}
