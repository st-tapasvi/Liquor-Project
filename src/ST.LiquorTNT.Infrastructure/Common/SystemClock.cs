using ST.LiquorTNT.Business.Common;

namespace ST.LiquorTNT.Infrastructure.Common;

public sealed class SystemClock : IClock
{
    // India runs one national timezone (UTC+5:30). Resolve it once; the id differs per OS.
    private static readonly TimeZoneInfo IndiaTimeZone = ResolveIndiaTimeZone();

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime IndiaNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IndiaTimeZone);

    private static TimeZoneInfo ResolveIndiaTimeZone()
    {
        // Windows uses "India Standard Time"; Linux/macOS use the IANA id "Asia/Kolkata".
        foreach (var id in new[] { "India Standard Time", "Asia/Kolkata" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        // Last resort so the app still runs on a host with no timezone database.
        return TimeZoneInfo.CreateCustomTimeZone("IST", TimeSpan.FromMinutes(330), "India Standard Time", "IST");
    }
}
