using Files.Tests.Lanes;
using WoW.Two.Sdk.Beta.Be.Files.Testing.Files;

namespace Files.Tests.Conformance.Files;

/// <summary>Form 1: the Files suite against the in-process module over a SQLite file, two fresh tenants per test.</summary>
public sealed class ModuleFilesConformanceTests(ModuleFilesSqliteFixture fixture) : FilesBrokerConformance, IClassFixture<ModuleFilesSqliteFixture>
{
    private readonly FilesBrokerSet brokers = fixture.For($"t-{Guid.NewGuid():N}"[..20]);
    private readonly FilesBrokerSet otherTenant = fixture.For($"t-{Guid.NewGuid():N}"[..20]);

    protected override FilesBrokerSet Brokers => brokers;

    protected override FilesBrokerSet OtherTenant => otherTenant;
}

/// <summary>Form 1: the same suite against the module over PostgreSQL; skipped without Docker.</summary>
[Collection(PostgresLane.Collection)]
public sealed class ModuleFilesPostgresConformanceTests(ModuleFilesPostgresFixture fixture) : FilesBrokerConformance, IClassFixture<ModuleFilesPostgresFixture>
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

/// <summary>Form 1: WebP variants through the in-process module over a SQLite file, a fresh tenant per test.</summary>
public sealed class ModuleFileVariantsConformanceTests(ModuleFilesSqliteFixture fixture) : FileVariantsConformance, IClassFixture<ModuleFilesSqliteFixture>
{
    private readonly FilesBrokerSet brokers = fixture.For($"v-{Guid.NewGuid():N}"[..20]);

    protected override FilesBrokerSet Brokers => brokers;
}

/// <summary>Form 1: the same variants through the module over PostgreSQL; skipped without Docker.</summary>
[Collection(PostgresLane.Collection)]
public sealed class ModuleFileVariantsPostgresConformanceTests(ModuleFilesPostgresFixture fixture) : FileVariantsConformance, IClassFixture<ModuleFilesPostgresFixture>
{
    private readonly FilesBrokerSet brokers = fixture.For($"v-{Guid.NewGuid():N}"[..20]);

    protected override FilesBrokerSet Brokers => brokers;
}

/// <summary>Forms 2 and 3: the same variants through the Client against the service host.</summary>
public sealed class ClientFileVariantsConformanceTests(ClientFilesFixture fixture) : FileVariantsConformance, IClassFixture<ClientFilesFixture>
{
    protected override FilesBrokerSet Brokers => fixture.Tenant;
}
