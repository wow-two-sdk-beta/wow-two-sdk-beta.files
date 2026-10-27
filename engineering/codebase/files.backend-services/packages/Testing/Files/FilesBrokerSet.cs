using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Uploads;

namespace WoW.Two.Sdk.Beta.Be.Files.Testing.Files;

/// <summary>Represents one caller's view of a Files implementation: the brokers as one tenant reaches them.</summary>
/// <param name="Uploads">The upload broker.</param>
/// <param name="Store">The store broker.</param>
public sealed record FilesBrokerSet(IFileUploadsBroker Uploads, IFileStoreBroker Store);
