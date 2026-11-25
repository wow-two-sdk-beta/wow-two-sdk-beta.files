using WoW.Two.Sdk.Beta.Be.Files.Store;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Delivery.Models;

internal sealed record FileDeliveryRead(DeliverableFileModel File, FileContent? Content, bool NotModified)
{
    internal bool HasUnsatisfiedRange => !NotModified && Content is null;
}
