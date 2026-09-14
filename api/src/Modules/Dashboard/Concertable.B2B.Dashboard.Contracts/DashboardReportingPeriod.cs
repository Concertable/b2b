namespace Concertable.B2B.Dashboard.Contracts;

public readonly record struct DashboardReportingPeriod(DateTime Now, DateTime MonthStart)
{
    public bool HasElapsedTime => Now > MonthStart;

    public static DashboardReportingPeriod From(DateTime now) =>
        new(now, new DateTime(now.Year, now.Month, 1, 0, 0, 0, now.Kind));
}
