using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Files.Tests.Lanes;

/// <summary>The xUnit framework of the DB-touching tiers: runs every test as xUnit does, except that it reports each test of
/// the PostgreSQL lane skipped, with the reason, while Docker is unreachable.</summary>
/// <remarks>xUnit 2 has no runtime skip, and the conformance suite's <c>[Fact]</c>s ship in a package, so the gate sits
/// in the executor: it swaps a lane test for a skipped one before the run. Each tier's csproj names it with
/// <c>[assembly: TestFramework]</c>.</remarks>
/// <param name="messageSink">The diagnostic sink xUnit passes.</param>
public sealed class FilesLaneTestFramework(IMessageSink messageSink) : XunitTestFramework(messageSink)
{
    /// <inheritdoc />
    protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName) =>
        new LaneExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);

    /// <summary>Runs the assembly's tests with the PostgreSQL lane gated on Docker.</summary>
    private sealed class LaneExecutor(AssemblyName assemblyName, ISourceInformationProvider sourceInformationProvider, IMessageSink diagnosticMessageSink)
        : XunitTestFrameworkExecutor(assemblyName, sourceInformationProvider, diagnosticMessageSink)
    {
        /// <inheritdoc />
        protected override void RunTestCases(IEnumerable<IXunitTestCase> testCases, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions) =>
            base.RunTestCases(testCases.Select(Gate).ToList(), executionMessageSink, executionOptions);

        private IXunitTestCase Gate(IXunitTestCase testCase) =>
            InPostgresLane(testCase.TestMethod.TestClass.Class) && PostgresLane.SkipReason is { } reason
                ? new XunitSkippedDataRowTestCase(DiagnosticMessageSink, TestMethodDisplay.ClassAndMethod, TestMethodDisplayOptions.None, testCase.TestMethod, reason, testCase.TestMethodArguments)
                : testCase;

        private static bool InPostgresLane(ITypeInfo testClass) =>
            testClass.GetCustomAttributes(typeof(CollectionAttribute))
                .Any(attribute => attribute.GetConstructorArguments().FirstOrDefault() as string == PostgresLane.Collection);
    }
}
