using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrobbleStatsMechanizerCommon
{
    public static class TagLibScrobbleStatsExtensions
    {
        public static int? GetWeightedRating(this TagLib.File tagLibFile)
        {
            return tagLibFile.GetCustomValue(TagCustomKey.WeightedRating).ReadNullableInt();
        }

        public static uint GetTimesSkipped(this TagLib.File tagLibFile)
        {
            return tagLibFile.GetCustomValue(TagCustomKey.TimesSkipped).ReadUInt();
        }

        public static uint GetTimesFinished(this TagLib.File tagLibFile)
        {
            return tagLibFile.GetCustomValue(TagCustomKey.TimesFinished).ReadUInt();
        }


        public static uint GetTimesPlayed(this TagLib.File tagLibFile)
        {
            return tagLibFile.GetTimesFinished() + tagLibFile.GetTimesSkipped();
        }

        public static DateTime? GetLastPlayed(this TagLib.File tagLibFile)
        {
            return tagLibFile.GetCustomValue(TagCustomKey.LastPlayed).ReadNullableUniversalDateTime();
        }

        public static double? DaysSinceLastPlayed(this TagLib.File tagLibFile)
        {
            var lastPlayed = tagLibFile.GetLastPlayed();

            if (!lastPlayed.HasValue)
            {
                return null;
            }

            return DateTime.Now.Subtract(lastPlayed.Value).TotalDays;
        }

        private const int NEUTRAL_RATING_THRESHOLD = 1000;

        /// <summary>
        /// TODO: Make this a calculated value
        /// </summary>
        private const int GOOD_RATING_THRESHOLD = 1080;

        public static bool RatingIsGoodOrBetter(this TagLib.File tagLibFile)
        {
            return tagLibFile.GetWeightedRating().HasValue && tagLibFile.GetWeightedRating().Value > GOOD_RATING_THRESHOLD;

        }
    }
}
