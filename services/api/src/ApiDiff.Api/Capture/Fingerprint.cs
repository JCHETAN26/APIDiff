using System.Security.Cryptography;
using System.Text;

namespace ApiDiff.Api.Capture;

/// <summary>
/// Computes a stable content hash for a (sanitized) scenario, used to detect and
/// cluster duplicates. Computed over sanitized data so equivalent requests share
/// a fingerprint.
/// </summary>
public static class Fingerprint
{
    /// <summary>Returns a lowercase hex SHA-256 over the request identity + body.</summary>
    public static string Compute(string method, string path, string query, ReadOnlySpan<byte> body)
    {
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hasher.AppendData(Encoding.UTF8.GetBytes($"{method.ToUpperInvariant()}\n{path}\n{query}\n"));
        hasher.AppendData(body);
        return Convert.ToHexStringLower(hasher.GetHashAndReset());
    }
}
