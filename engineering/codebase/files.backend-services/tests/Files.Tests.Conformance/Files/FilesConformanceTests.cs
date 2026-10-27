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

/// <summary>Forms 2 and 3: the same suite through the Client against the service host, by two tenants' API keys.</summary>
public sealed class ClientFilesConformanceTests(ClientFilesFixture fixture) : FilesBrokerConformance, IClassFixture<ClientFilesFixture>
{
    protected override FilesBrokerSet Brokers => fixture.Tenant;

    protected override FilesBrokerSet OtherTenant => fixture.OtherTenant;

    protected override TimeProvider Clock => fixture.Clock;
}
