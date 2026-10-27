using WoW.Two.Sdk.Beta.Be.Files.Testing.Files;

namespace Files.Tests.Conformance.Files;

/// <summary>Form 1: the Files suite against the in-process module, two fresh tenants per test.</summary>
public sealed class ModuleFilesConformanceTests(ModuleFilesFixture fixture) : FilesBrokerConformance, IClassFixture<ModuleFilesFixture>
{
    private readonly FilesBrokerSet brokers = fixture.For($"t-{Guid.NewGuid():N}"[..20]);
    private readonly FilesBrokerSet otherTenant = fixture.For($"t-{Guid.NewGuid():N}"[..20]);

    protected override FilesBrokerSet Brokers => brokers;

    protected override FilesBrokerSet OtherTenant => otherTenant;
}
