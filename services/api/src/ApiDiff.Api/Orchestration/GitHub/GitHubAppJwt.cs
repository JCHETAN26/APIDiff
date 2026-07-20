using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ApiDiff.Api.Orchestration.GitHub;

/// <summary>
/// Builds the short-lived RS256 JWT a GitHub App presents to obtain an
/// installation access token. Pure crypto — no network — so it is unit-testable.
/// </summary>
public static class GitHubAppJwt
{
    public static string Create(string appId, RSA privateKey, DateTimeOffset now)
    {
        // GitHub allows up to 10 minutes; back-date iat 60s to tolerate clock skew.
        var header = new { alg = "RS256", typ = "JWT" };
        var payload = new
        {
            iat = now.AddSeconds(-60).ToUnixTimeSeconds(),
            exp = now.AddMinutes(9).ToUnixTimeSeconds(),
            iss = appId,
        };

        var signingInput =
            $"{Base64Url(JsonSerializer.SerializeToUtf8Bytes(header))}.{Base64Url(JsonSerializer.SerializeToUtf8Bytes(payload))}";
        var signature = privateKey.SignData(
            Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return $"{signingInput}.{Base64Url(signature)}";
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
