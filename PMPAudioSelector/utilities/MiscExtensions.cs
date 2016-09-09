using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMPAudioSelector
{
    public static class Utilities
    {
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
