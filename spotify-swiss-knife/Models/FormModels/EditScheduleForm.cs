namespace spotify_swiss_knife.Models.FormModels;

/// <summary>
/// Form for editing an existing <see cref="ScheduledShuffle"/>. Shares all of
/// <see cref="CreateScheduleForm"/>'s fields and conversion logic, adding the <see cref="Id"/>
/// of the schedule being edited. The schedule's stored UTC cron is decoded into this form's
/// fields (as UTC values) by <see cref="CronScheduleDecoder"/>; the view localizes them for
/// display, and on submit the inherited <see cref="CreateScheduleForm.ToCronExpression"/>
/// converts the user's local selection back to a UTC cron exactly as creation does.
/// </summary>
public sealed class EditScheduleForm : CreateScheduleForm
{
    public int Id { get; set; }
}
