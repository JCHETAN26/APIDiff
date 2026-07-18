namespace ApiDiff.Api.Capture;

/// <summary>
/// Configures capture-time redaction. Defaults are conservative: headers use an
/// allowlist (anything not listed is dropped), and bodies/queries are scrubbed
/// for sensitive keys and known PII/secret value patterns.
/// </summary>
public sealed class SanitizationOptions
{
    /// <summary>Replacement written in place of any redacted value.</summary>
    public string Placeholder { get; init; } = "[REDACTED]";

    /// <summary>Header names (case-insensitive) that are safe to retain. Others are dropped.</summary>
    public IReadOnlySet<string> HeaderAllowlist { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "accept",
        "accept-encoding",
        "accept-language",
        "content-type",
        "content-length",
        "user-agent",
        "cache-control",
        "x-request-id",
        "x-correlation-id",
        "x-trace-id",
    };

    /// <summary>
    /// Sensitive field/param names. Matching is done on the name lowercased with
    /// separators removed; a value is redacted wholesale when its key matches.
    /// </summary>
    public IReadOnlySet<string> SensitiveKeys { get; init; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "password", "passwd", "pwd", "secret", "clientsecret", "token", "auth",
        "authorization", "apikey", "accesstoken", "refreshtoken", "idtoken",
        "privatekey", "ssn", "socialsecurity", "cvv", "cvc", "pin", "cardnumber",
        "creditcard", "sessionid", "cookie", "securitycode",
    };

    /// <summary>
    /// Long, unambiguous tokens matched as substrings of a normalized key (to
    /// catch compound names like <c>userPassword</c> or <c>api_key</c>).
    /// </summary>
    public IReadOnlyList<string> SensitiveKeySubstrings { get; init; } =
    [
        "password", "secret", "apikey", "accesstoken", "refreshtoken",
        "privatekey", "creditcard", "cardnumber", "authorization",
    ];

    public static SanitizationOptions Default { get; } = new();
}
