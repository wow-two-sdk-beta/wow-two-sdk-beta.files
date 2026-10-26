using WoW.Two.Sdk.Beta.Be.Files.Greetings;

namespace WoW.Two.Sdk.Beta.Be.Files.Client.Greetings;

/// <summary>Represents the service's success envelope around a greeting.</summary>
/// <param name="Data">The greeting.</param>
internal sealed record GreetingEnvelope(GreetingModel? Data);
