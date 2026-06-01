using System;

namespace AutoWashPro.BLL.Helpers
{
    public static class DateTimeExtensions
    {
        public static DateTime ToVnTime(this DateTime utcDate)
        {
            TimeZoneInfo vnTimeZone;
            try
            {
                vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            }
            return TimeZoneInfo.ConvertTimeFromUtc(utcDate, vnTimeZone);
        }
    }
}
