namespace Files.Api.Migrate.Input;

/// <summary>Signals input the mover refuses — bad standard input, an unknown kind, a missing field, a missing source
/// folder or file — so the run exits <see cref="MigrateExitCodeConstants.Refused"/>.</summary>
/// <remarks>Its message names fields and kinds only, never a value that could hold a secret.</remarks>
/// <param name="message">The reason, written to standard error.</param>
public sealed class MigrateInputException(string message) : Exception(message);
