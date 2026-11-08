using Files.Api.Configurations;
using Files.Api.Migrate;
using Files.Api.Migrate.Input;

// The mover: `migrate plan|push|verify` reads one JSON document on standard input and exits before any web host is
// built or any settings file read.
if (args is [MigrateVerbConstants.Command, .. var verb])
    return await MigrateCommand.RunAsync(verb, Console.In, Console.Out, Console.Error);

// Build and configure the host.
var builder = WebApplication.CreateBuilder(args);
builder.Configure();

// Build and configure the app.
var app = builder.Build();
await app.ConfigureAsync();

// Run.
app.Run();
return 0;

public partial class Program;
