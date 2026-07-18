using System.Text.RegularExpressions;

namespace ApiDiff.Api.Common;

/// <summary>Validation for URL-safe slugs (lowercase alphanumerics and hyphens).</summary>
public static partial class Slug
{
    [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]{0,98}[a-z0-9])?$")]
    private static partial Regex Pattern();

    public static bool IsValid(string? value) => value is not null && Pattern().IsMatch(value);
}
