using System;
using System.Globalization;

namespace WorkflowLib;

public static class DateTimeUtils
{
    public static int GetWeekOfYear(this DateTime date)
    {
        CultureInfo culture = CultureInfo.CurrentCulture;
        Calendar calendar = culture.Calendar;
        return calendar.GetWeekOfYear(date, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
    }
}
