namespace spotify_swiss_knife.Models.FormModels;

/// <summary>
/// Inverse of <see cref="CreateScheduleForm.ToCronExpression"/>: decodes a stored 5-field UTC
/// cron string back into the <see cref="ShuffleFrequency"/> and the UTC day/time fields the
/// schedule form binds to. Only the limited cron shapes the form itself produces are recognised:
/// <c>m h * * *</c> (Daily), <c>m h * * d[,d…]</c> (Weekly / CustomWeekly) and
/// <c>m h dom * *</c> (Monthly). The returned day-of-week and time values are in UTC — the view
/// converts them to the viewer's local time before display, mirroring how creation converts
/// local input to UTC.
/// </summary>
public static class CronScheduleDecoder
{
    /// <summary>The UTC schedule fields recovered from a cron string.</summary>
    public sealed record Decoded(
        ShuffleFrequency Frequency,
        int Hour,
        int Minute,
        IReadOnlyList<int> DaysOfWeek,
        int DayOfMonth);

    // A sensible fallback so the edit form is always usable even if a cron string was hand-edited
    // into a shape the form can't represent: weekly, Monday, 08:00 UTC.
    private static Decoded Fallback() =>
        new(ShuffleFrequency.Weekly, 8, 0, new[] { 1 }, 1);

    public static Decoded Decode(string? cron)
    {
        if (string.IsNullOrWhiteSpace(cron))
            return Fallback();

        var parts = cron.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5)
            return Fallback();

        if (!int.TryParse(parts[0], out var minute) || minute is < 0 or > 59 ||
            !int.TryParse(parts[1], out var hour) || hour is < 0 or > 23)
            return Fallback();

        var dayOfMonthField = parts[2];
        var dayOfWeekField = parts[4];

        // Monthly: "m h dom * *".
        if (dayOfMonthField != "*")
        {
            if (int.TryParse(dayOfMonthField, out var dom) && dom is >= 1 and <= 31)
                return new Decoded(ShuffleFrequency.Monthly, hour, minute, Array.Empty<int>(), dom);
            return Fallback();
        }

        // Daily: "m h * * *".
        if (dayOfWeekField == "*")
            return new Decoded(ShuffleFrequency.Daily, hour, minute, Array.Empty<int>(), 1);

        // Weekly / CustomWeekly: "m h * * d[,d…]".
        var days = new List<int>();
        foreach (var token in dayOfWeekField.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!int.TryParse(token, out var d) || d is < 0 or > 6)
                return Fallback();
            if (!days.Contains(d))
                days.Add(d);
        }

        if (days.Count == 0)
            return Fallback();

        days.Sort();
        var frequency = days.Count == 1 ? ShuffleFrequency.Weekly : ShuffleFrequency.CustomWeekly;
        return new Decoded(frequency, hour, minute, days, 1);
    }
}
