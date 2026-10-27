using WoW.Two.Sdk.Beta.Be.Files.Audit;
using WoW.Two.Sdk.Beta.Be.Files.References;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Uploads;
using WoW.Two.Sdk.Beta.Be.Files.Urls;

namespace WoW.Two.Sdk.Beta.Be.Files.Testing.Files;

/// <summary>Represents one caller's view of a Files implementation: the brokers as one tenant reaches them.</summary>
/// <param name="Uploads">The upload broker.</param>
/// <param name="Store">The store broker.</param>
/// <param name="References">The references broker.</param>
/// <param name="Urls">The URLs broker.</param>
/// <param name="Audit">The audit broker.</param>
public sealed record FilesBrokerSet(IFileUploadsBroker Uploads, IFileStoreBroker Store, IFileReferencesBroker References, IFileUrlsBroker Urls, IFileAuditBroker Audit);
