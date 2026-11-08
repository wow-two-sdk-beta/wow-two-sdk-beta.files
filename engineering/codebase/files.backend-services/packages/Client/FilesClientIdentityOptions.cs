using WoW.Two.Sdk.Beta.Be.Http.Auth.OAuth2;

namespace WoW.Two.Sdk.Beta.Be.Files.Client;

/// <summary>Holds the Identity client the Files client gets its tokens from, with the be sdk's own OAuth2 settings: the
/// token endpoint, the client id, <see cref="OAuth2TokenClientOptions.ClientSecret"/> or
/// <see cref="OAuth2TokenClientOptions.ClientPrivateKey"/>, and the resource.</summary>
/// <remarks>A null <see cref="OAuth2TokenClientOptions.Resource"/> asks for <see cref="FilesClientOptions.BaseAddress"/>
/// with its trailing slash, the audience Files accepts. The be sdk handlers fetch, cache and renew every token; the Files
/// client keeps none.</remarks>
public sealed record FilesClientIdentityOptions : OAuth2TokenClientOptions
{
    /// <summary>Copies these settings onto a be sdk grant's options, the resource defaulting to the service root.</summary>
    internal void CopyTo(OAuth2TokenClientOptions target, Uri baseAddress)
    {
        target.TokenEndpoint = TokenEndpoint;
        target.ClientId = ClientId;
        target.ClientSecret = ClientSecret;
        target.ClientPrivateKey = ClientPrivateKey;
        target.ClientPrivateKeyId = ClientPrivateKeyId;
        target.ClientAssertionAudience = ClientAssertionAudience;
        target.Resource = string.IsNullOrWhiteSpace(Resource) ? DefaultResource(baseAddress) : Resource;
        target.Scope = Scope;
        target.RefreshSkew = RefreshSkew;
        foreach (var parameter in AdditionalParameters)
            target.AdditionalParameters[parameter.Key] = parameter.Value;
    }

    /// <summary>Returns the service root as a resource URI: <see cref="Uri.AbsoluteUri"/> with its trailing slash.</summary>
    internal static string DefaultResource(Uri baseAddress)
    {
        var resource = baseAddress.AbsoluteUri;
        return resource.EndsWith('/') ? resource : resource + "/";
    }
}
