using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ApiDiff.Api.Capture;

/// <summary>Redacts PII and secrets from captured traffic before it is stored.</summary>
public interface ISanitizer
{
    /// <summary>Keeps only allowlisted headers (values scrubbed); returns a JSON object.</summary>
    string SanitizeHeadersToJson(IReadOnlyDictionary<string, string> headers);

    /// <summary>Scrubs a request/response body, dispatching on content type.</summary>
    byte[] SanitizeBody(string? contentType, string? body);

    /// <summary>Scrubs a raw query string (without the leading '?').</summary>
    string SanitizeQuery(string? query);

    /// <summary>Applies value-level PII/secret patterns to arbitrary text.</summary>
    string RedactText(string input);
}

public sealed partial class Sanitizer(SanitizationOptions options) : ISanitizer
{
    private readonly Regex[] _valuePatterns =
    [
        EmailRegex(), JwtRegex(), BearerRegex(), CreditCardRegex(), SsnRegex(), AwsKeyRegex(),
    ];

    public string SanitizeHeadersToJson(IReadOnlyDictionary<string, string> headers)
    {
        var kept = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in headers)
        {
            if (options.HeaderAllowlist.Contains(name))
            {
                kept[name] = RedactText(value);
            }
        }

        return JsonSerializer.Serialize(kept);
    }

    public byte[] SanitizeBody(string? contentType, string? body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return [];
        }

        string sanitized;
        if (contentType is not null && contentType.Contains("x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
        {
            sanitized = SanitizeQuery(body);
        }
        else if (TryParseJson(body, out var node))
        {
            sanitized = SanitizeJson(node)?.ToJsonString() ?? body;
        }
        else
        {
            sanitized = RedactText(body);
        }

        return Encoding.UTF8.GetBytes(sanitized);
    }

    public string SanitizeQuery(string? query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return "";
        }

        var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < pairs.Length; i++)
        {
            var eq = pairs[i].IndexOf('=');
            if (eq < 0)
            {
                continue;
            }

            var key = pairs[i][..eq];
            var value = pairs[i][(eq + 1)..];
            var newValue = IsSensitiveKey(key) ? options.Placeholder : RedactText(value);
            pairs[i] = $"{key}={newValue}";
        }

        return string.Join('&', pairs);
    }

    public string RedactText(string input)
    {
        var result = input;
        foreach (var pattern in _valuePatterns)
        {
            result = pattern.Replace(result, options.Placeholder);
        }

        return result;
    }

    private JsonNode? SanitizeJson(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                var newObj = new JsonObject();
                foreach (var (key, value) in obj)
                {
                    newObj[key] = IsSensitiveKey(key) ? JsonValue.Create(options.Placeholder) : SanitizeJson(value);
                }

                return newObj;

            case JsonArray arr:
                var newArr = new JsonArray();
                foreach (var element in arr)
                {
                    newArr.Add(SanitizeJson(element));
                }

                return newArr;

            case JsonValue val:
                return val.TryGetValue<string>(out var s)
                    ? JsonValue.Create(RedactText(s))
                    : JsonNode.Parse(val.ToJsonString());

            default:
                return null;
        }
    }

    private bool IsSensitiveKey(string key)
    {
        var normalized = Normalize(key);
        if (options.SensitiveKeys.Contains(normalized))
        {
            return true;
        }

        foreach (var token in options.SensitiveKeySubstrings)
        {
            if (normalized.Contains(token, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string Normalize(string key)
    {
        var sb = new StringBuilder(key.Length);
        foreach (var c in key)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(char.ToLowerInvariant(c));
            }
        }

        return sb.ToString();
    }

    private static bool TryParseJson(string body, out JsonNode? node)
    {
        node = null;
        try
        {
            node = JsonNode.Parse(body);
            return node is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\beyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\b")]
    private static partial Regex JwtRegex();

    [GeneratedRegex(@"(?i)\bBearer\s+[A-Za-z0-9\-._~+/]+=*")]
    private static partial Regex BearerRegex();

    [GeneratedRegex(@"\b\d{4}(?:[ -]?\d{4}){2,3}\b")]
    private static partial Regex CreditCardRegex();

    [GeneratedRegex(@"\b\d{3}-\d{2}-\d{4}\b")]
    private static partial Regex SsnRegex();

    [GeneratedRegex(@"\bAKIA[0-9A-Z]{16}\b")]
    private static partial Regex AwsKeyRegex();
}
