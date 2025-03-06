using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caspar
{
    static public partial class Api
    {
        public static class DateHelper
        {
            private static int GetOffsetDayOfWeek(DayOfWeek dayOfWeek, DayOfWeek startDayOfWeek)
            {
                return (((int)dayOfWeek - (int)startDayOfWeek + 7) % 7);
            }

            public static bool IsSameWeek(DateTime date1, DateTime date2, DayOfWeek weekStartsOn)
            {
                return date1.AddDays(-GetOffsetDayOfWeek(date1.DayOfWeek, weekStartsOn)).Date == date2.AddDays(-GetOffsetDayOfWeek(date2.DayOfWeek, weekStartsOn)).Date;
            }

            public static DateTime GetFirstDateTimeOfWeek(DateTime date, DayOfWeek firstDayOfWeek)
            {
                int offset = GetOffsetDayOfWeek(date.DayOfWeek, firstDayOfWeek);

                return date.AddDays(-offset);

            }

            public static DateTime GetFirstDateTimeOfMonth(DateTime date)
            {
                return date.AddDays(-(date.Day - 1)); ;

            }

            public static DayOfWeek FirstDayOfWeek { get; set; } = DayOfWeek.Monday;

            public static void GetDiffDayWeekMonthWithNow(DateTime target, ref bool diffDay, ref bool diffWeek, ref bool diffMonth)
            {
                if (target.Year != DateTime.UtcNow.Year ||
                    target.Month != DateTime.UtcNow.Month ||
                    target.Day != DateTime.UtcNow.Day)
                {
                    diffDay = true;
                }

                if (DateHelper.IsSameWeek(target, DateTime.UtcNow, DateHelper.FirstDayOfWeek) == false)
                {
                    diffWeek = true;
                }

                if (target.Year != DateTime.UtcNow.Year ||
                    target.Month != DateTime.UtcNow.Month)
                {
                    diffMonth = true;
                }
            }
            public static int GetDifferencesDays(DateTime start, DateTime end)
            {
                TimeSpan startTimeSpan = new TimeSpan(start.Ticks);
                TimeSpan endTimeSpan = new TimeSpan(end.Ticks);

                return Math.Abs(endTimeSpan.Days - startTimeSpan.Days);
            }

            public static int GetDifferencesSeconds(DateTime start, DateTime end)
            {

                TimeSpan startTimeSpan = new TimeSpan(start.Ticks);
                TimeSpan endTimeSpan = new TimeSpan(end.Ticks);

                return Math.Abs((int)(endTimeSpan.TotalSeconds - startTimeSpan.TotalSeconds));
            }

            public static int GetDifferencesMinutes(DateTime start, DateTime end)
            {

                TimeSpan startTimeSpan = new TimeSpan(start.Ticks);
                TimeSpan endTimeSpan = new TimeSpan(end.Ticks);

                return Math.Abs((int)(endTimeSpan.TotalMinutes - startTimeSpan.TotalMinutes));
            }
        }
    }

}
