using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorLibrary.Helpers
{
    public static class TimeSpanExtensions
    {
        public static TimeSpan LocalTimeSpanToUTC(this TimeSpan tsLocal)
        {
            DateTime dt = DateTime.Now.Date.Add(tsLocal);
            return dt.ToUniversalTime().TimeOfDay;
        }

        public static TimeSpan UTCTimeSpanToLocal(this TimeSpan tsUtc)
        {
            DateTime dtUtc = DateTime.UtcNow.Date.Add(tsUtc);
            return dtUtc.ToLocalTime().TimeOfDay;
        }
    }
}
