using System.Security.Cryptography;
using System.Text;
using ApiDiff.Api.Webhooks;
using Xunit;

namespace ApiDiff.Api.Tests;

public class GitHubSignatureVerifierTests
{
    private const string Secret = "s3cr3t";

    private static string Sign(string secret, string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return "sha256=" + Convert.ToHexStringLower(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    [Fact]
    public void ValidSignature_Passes()
    {
        const string payload = """{"hello":"world"}""";
        var header = Sign(Secret, payload);
        Assert.True(GitHubSignatureVerifier.IsValid(Secret, Encoding.UTF8.GetBytes(payload), header));
    }

    [Fact]
    public void WrongSecret_Fails()
    {
        const string payload = """{"hello":"world"}""";
        var header = Sign("other", payload);
        Assert.False(GitHubSignatureVerifier.IsValid(Secret, Encoding.UTF8.GetBytes(payload), header));
    }

    [Theory]
    [InlineData("")]
    [InlineData("deadbeef")]
    [InlineData("sha256=nothex")]
    [InlineData("sha1=abcd")]
    public void MalformedOrMissing_Fails(string header)
    {
        Assert.False(GitHubSignatureVerifier.IsValid(Secret, "payload"u8, header));
    }

    [Fact]
    public void EmptySecret_Fails()
    {
        const string payload = "x";
        Assert.False(GitHubSignatureVerifier.IsValid("", Encoding.UTF8.GetBytes(payload), Sign("", payload)));
    }
}
