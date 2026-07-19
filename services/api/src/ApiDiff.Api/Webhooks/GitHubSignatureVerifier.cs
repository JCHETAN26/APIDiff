using System.Security.Cryptography;
using System.Text;

namespace ApiDiff.Api.Webhooks;

/// <summary>Verifies GitHub webhook payloads via their HMAC-SHA256 signature.</summary>
public static class GitHubSignatureVerifier
{
    private const string Prefix = "sha256=";

    /// <summary>
    /// Returns true when <paramref name="signatureHeader"/> (the value of
    /// <c>X-Hub-Signature-256</c>) is a valid HMAC-SHA256 of the payload under
    /// the shared secret. Comparison is constant-time.
    /// </summary>
    public static bool IsValid(string secret, ReadOnlySpan<byte> payload, string? signatureHeader)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(signatureHeader) ||
            !signatureHeader.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        byte[] provided;
        try
        {
            provided = Convert.FromHexString(signatureHeader[Prefix.Length..]);
        }
        catch (FormatException)
        {
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expected = hmac.ComputeHash(payload.ToArray());

        return CryptographicOperations.FixedTimeEquals(expected, provided);
    }
}
