namespace spotify_swiss_knife.Infrastructure;

// Central policy for writing personally identifiable values to logs. Email addresses are
// masked to first-character + domain so logs stay useful for support and debugging without
// persisting full addresses (and without handing an attacker a clean list of valid accounts
// from failed-login lines). Stable, non-identifying surrogate keys such as user ids are
// preferred over emails and are logged as-is.
public static class LogScrub
{
    // Masks an email to "a***@example.com". Returns "unknown" for empty input and "***" for
    // anything that isn't shaped like an email.
    public static string Email(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "unknown";

        var at = email.IndexOf('@');
        if (at <= 0 || at == email.Length - 1)
            return "***";

        return $"{email[0]}***@{email[(at + 1)..]}";
    }

    // Masks a logged actor name: emails are masked, other identifiers (user ids, display
    // names, the "anonymous"/"spotify-user" fallbacks) pass through unchanged.
    public static string User(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "unknown";

        return value.Contains('@') ? Email(value) : value;
    }
}
