using System.Text;
using ApiDiff.Api.Capture;
using Xunit;

namespace ApiDiff.Api.Tests;

public class SanitizerTests
{
    private readonly Sanitizer _sanitizer = new(SanitizationOptions.Default);

    /// <summary>A battery of secrets that must never survive sanitization.</summary>
    public static readonly string[] Secrets =
    [
        "alice@secret.example.com",
        "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTYifQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
        "4111 1111 1111 1111",
        "123-45-6789",
        "AKIAIOSFODNN7EXAMPLE",
        "hunter2super",
    ];

    [Fact]
    public void Headers_KeepOnlyAllowlisted()
    {
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer secrettoken",
            ["Cookie"] = "session=abc",
            ["X-Api-Key"] = "AKIAIOSFODNN7EXAMPLE",
            ["Content-Type"] = "application/json",
            ["User-Agent"] = "curl/8.0",
        };

        var json = _sanitizer.SanitizeHeadersToJson(headers);

        Assert.Contains("application/json", json);
        Assert.Contains("curl/8.0", json);
        Assert.DoesNotContain("Authorization", json);
        Assert.DoesNotContain("secrettoken", json);
        Assert.DoesNotContain("Cookie", json);
        Assert.DoesNotContain("AKIAIOSFODNN7EXAMPLE", json);
    }

    [Fact]
    public void JsonBody_RedactsSensitiveKeysAndValues_PreservesBenign()
    {
        const string body = """
        {
          "name": "Alice",
          "orderId": "A-100",
          "password": "hunter2super",
          "profile": { "email": "alice@secret.example.com", "ssn": "123-45-6789" },
          "cards": ["4111 1111 1111 1111"]
        }
        """;

        var result = Encoding.UTF8.GetString(_sanitizer.SanitizeBody("application/json", body));

        Assert.Contains("Alice", result);
        Assert.Contains("A-100", result);
        foreach (var secret in Secrets)
        {
            Assert.DoesNotContain(secret, result);
        }
    }

    [Fact]
    public void FormBody_RedactsSensitiveParams()
    {
        var result = Encoding.UTF8.GetString(
            _sanitizer.SanitizeBody("application/x-www-form-urlencoded", "user=alice&password=hunter2super&token=xyz"));

        Assert.Contains("user=alice", result);
        Assert.DoesNotContain("hunter2super", result);
        Assert.DoesNotContain("xyz", result);
    }

    [Fact]
    public void TextBody_RedactsPatterns()
    {
        var result = Encoding.UTF8.GetString(
            _sanitizer.SanitizeBody("text/plain", "contact alice@secret.example.com or card 4111 1111 1111 1111"));

        Assert.DoesNotContain("alice@secret.example.com", result);
        Assert.DoesNotContain("4111 1111 1111 1111", result);
        Assert.Contains("[REDACTED]", result);
    }

    [Fact]
    public void Query_RedactsSensitiveParams()
    {
        var result = _sanitizer.SanitizeQuery("page=2&api_key=AKIAIOSFODNN7EXAMPLE&q=widgets");

        Assert.Contains("page=2", result);
        Assert.Contains("q=widgets", result);
        Assert.DoesNotContain("AKIAIOSFODNN7EXAMPLE", result);
    }

    [Fact]
    public void EmptyBody_ReturnsEmpty()
    {
        Assert.Empty(_sanitizer.SanitizeBody("application/json", null));
        Assert.Empty(_sanitizer.SanitizeBody(null, ""));
    }
}
