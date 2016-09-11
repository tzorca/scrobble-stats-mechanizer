using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrobbleStatsMechanizerCommon
{
    public static class Utilities
    {
        /// <summary>
        /// Sourced from http://stackoverflow.com/a/24316350
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="f"></param>
        /// <returns></returns>
        public static Func<T, bool> Not<T>(this Func<T, bool> f)
        {
            return x => !f(x);
        }
        public static string FormatAs_yyyyMMdd(this DateTime dateTime)
        {
            return dateTime.ToString("yyyyMMdd");
        }
        public static List<string> SplitIntoLines(this string input)
        {
            return (input ?? "")
                .Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)
                .ToList();
        }
        public static T RemoveRandomElement<T>(this List<T> list, Random random)
        {
            var randomIndex = random.Next(list.Count);
            var randomElement = list[randomIndex];
            list.RemoveAt(randomIndex);

            return randomElement;
        }

        internal static uint ReadUInt(this string input)
        {
            uint val;
            if (uint.TryParse(input, out val))
            {
                return val;
            }
            else
            {
                return default(uint);
            }
        }


        internal static int? ReadNullableInt(this string input)
        {
            int val;
            if (int.TryParse(input, out val))
            {
                return val;
            }
            else
            {
                return null;
            }
        }

        internal static DateTime? ReadNullableUniversalDateTime(this string input)
        {
            DateTime val;
            if (DateTime.TryParseExact(input, "u", CultureInfo.InvariantCulture, DateTimeStyles.None, out val))
            {
                return val;
            }
            else
            {
                return null;
            }
        }
    }
}
