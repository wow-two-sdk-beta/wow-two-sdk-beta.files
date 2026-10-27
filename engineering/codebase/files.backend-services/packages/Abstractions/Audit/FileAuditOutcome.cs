namespace WoW.Two.Sdk.Beta.Be.Files.Audit;

/// <summary>Refers to how an audited call ended.</summary>
public enum FileAuditOutcome
{
    /// <summary>The call succeeded.</summary>
    Ok = 0,

    /// <summary>A check refused the call: size, type, content, policy.</summary>
    Refused,
}
