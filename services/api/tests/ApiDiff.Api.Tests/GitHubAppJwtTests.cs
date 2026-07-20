using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ApiDiff.Api.Orchestration.GitHub;
using Xunit;

namespace ApiDiff.Api.Tests;

public class GitHubAppJwtTests
{
    private static byte[] Base64UrlDecode(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        s = (s.Length % 4) switch { 2 => s + "==", 3 => s + "=", _ => s };
        return Convert.FromBase64String(s);
    }

    [Fact]
    public void Create_ProducesVerifiableRs256Jwt()
    {
        using var rsa = RSA.Create(2048);
        var now = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);

        var jwt = GitHubAppJwt.Create("12345", rsa, now);
        var parts = jwt.Split('.');
        Assert.Equal(3, parts.Length);

        using var header = JsonDocument.Parse(Base64UrlDecode(parts[0]));
        Assert.Equal("RS256", header.RootElement.GetProperty("alg").GetString());

        using var payload = JsonDocument.Parse(Base64UrlDecode(parts[1]));
        Assert.Equal("12345", payload.RootElement.GetProperty("iss").GetString());
        // iat is back-dated for clock skew; exp is within GitHub's 10-minute cap.
        Assert.Equal(now.AddSeconds(-60).ToUnixTimeSeconds(), payload.RootElement.GetProperty("iat").GetInt64());
        Assert.True(payload.RootElement.GetProperty("exp").GetInt64() <= now.AddMinutes(10).ToUnixTimeSeconds());

        var signingInput = Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}");
        var valid = rsa.VerifyData(signingInput, Base64UrlDecode(parts[2]), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        Assert.True(valid);
    }
}
