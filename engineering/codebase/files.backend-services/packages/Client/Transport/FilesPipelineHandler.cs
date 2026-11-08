namespace WoW.Two.Sdk.Beta.Be.Files.Client.Transport;

/// <summary>Ends a token client's pipeline in the Files client's own, so the handlers and the primary handler set on
/// <see cref="FilesClientServiceCollectionExtensions.AddFilesClient"/>'s builder serve token calls too.</summary>
internal sealed class FilesPipelineHandler(IHttpMessageHandlerFactory handlers) : HttpMessageHandler
{
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // An invoker, not an HttpClient: the request was sent once already, and only HttpClient refuses a second send.
        using var invoker = new HttpMessageInvoker(handlers.CreateHandler(FilesClientConstants.HttpClientName), disposeHandler: false);
        return await invoker.SendAsync(request, cancellationToken);
    }
}
