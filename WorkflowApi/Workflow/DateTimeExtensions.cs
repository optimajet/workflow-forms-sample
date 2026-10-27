using System.Globalization;

namespace WorkflowApi.Workflow;

internal static class DateTimeExtensions
{
    public static int GetWeekOfYear(this DateTime date)
    {
        var culture = CultureInfo.CurrentCulture;
        return culture.Calendar.GetWeekOfYear(
            date,
            culture.DateTimeFormat.CalendarWeekRule,
            culture.DateTimeFormat.FirstDayOfWeek);
    }
}
