using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

/// <summary>
/// Form for creating a <see cref="ScheduledShuffle"/>. Collects the playlist, a frequency and
/// the relevant day fields, and a local time; <see cref="ToCronExpression"/> converts those to
/// the UTC cron string that gets stored. <see cref="Validate"/> enforces the per-frequency day
/// rules.
/// </summary>
public class CreateScheduleForm : IValidatableObject
{
    [Required]
    public string PlaylistId { get; set; } = string.Empty;

    [Required]
    public string PlaylistName { get; set; } = string.Empty;

    // How often the shuffle runs. Determines which day field is honored:
    // Daily ignores both; Weekly/CustomWeekly use DaysOfWeek; Monthly uses DayOfMonth.
    public ShuffleFrequency? Frequency { get; set; }

    // One or more days-of-week (0 = Sunday … 6 = Saturday).
    // Weekly requires exactly one; CustomWeekly requires at least one.
    public List<int> DaysOfWeek { get; set; } = [];

    // Calendar day of the month (1–31), used only when Frequency is Monthly.
    public int? DayOfMonth { get; set; }

    // "HH:mm" as entered in the client's local timezone, e.g. "08:00".
    // Converted to UTC in ToCronExpression using TimezoneOffsetMinutes.
    [Required]
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Enter a valid time (HH:mm).")]
    public string TimeUtc { get; set; } = "08:00";

    // Browser Date.getTimezoneOffset(): minutes to add to local time to reach UTC.
    // Null when unavailable (e.g. no JS), in which case the time is treated as UTC.
    public int? TimezoneOffsetMinutes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var frequency = Frequency ?? ShuffleFrequency.Weekly;

        switch (frequency)
        {
            case ShuffleFrequency.Weekly:
                if (DaysOfWeek.Count != 1)
                    yield return new ValidationResult("Select exactly one day.", [nameof(DaysOfWeek)]);
                break;
            case ShuffleFrequency.CustomWeekly:
                if (DaysOfWeek.Count < 1)
                    yield return new ValidationResult("Select at least one day.", [nameof(DaysOfWeek)]);
                break;
            case ShuffleFrequency.Monthly:
                if (DayOfMonth is null or < 1 or > 31)
                    yield return new ValidationResult("Select a day of the month (1–31).", [nameof(DayOfMonth)]);
                break;
        }
    }

    // Builds the 5-field UTC cron string, converting the submitted local time
    // and days to UTC. A conversion that crosses midnight shifts the day.
    public string ToCronExpression()
    {
        var parts = TimeUtc.Split(':');
        var localMinutes = int.Parse(parts[0]) * 60 + int.Parse(parts[1]);
        var total = localMinutes + (TimezoneOffsetMinutes ?? 0);

        // Offset is within ±14h, so the day shift is always -1, 0 or +1.
        var dayDelta = (int)Math.Floor(total / 1440.0);
        var utcMinutes = total - dayDelta * 1440;

        var hour = (utcMinutes / 60).ToString("D2");
        var minute = (utcMinutes % 60).ToString("D2");

        var frequency = Frequency ?? ShuffleFrequency.Weekly;

        return frequency switch
        {
            ShuffleFrequency.Daily => $"{minute} {hour} * * *",
            ShuffleFrequency.Monthly => $"{minute} {hour} {ShiftDayOfMonth(DayOfMonth ?? 1, dayDelta)} * *",
            _ => $"{minute} {hour} * * {ShiftDaysOfWeek(dayDelta)}",
        };
    }

    private string ShiftDaysOfWeek(int dayDelta) =>
        string.Join(",", DaysOfWeek
            .Select(d => (((d + dayDelta) % 7) + 7) % 7)
            .Distinct()
            .OrderBy(d => d));

    // Applies the UTC day shift to a day-of-month, wrapping within 1–31.
    // This is an approximation across the month boundary (month lengths vary),
    // but the shift only triggers when the local time is within the timezone
    // offset of midnight, so it's a rare edge case.
    private static int ShiftDayOfMonth(int day, int dayDelta) =>
        (((day - 1 + dayDelta) % 31) + 31) % 31 + 1;
}
