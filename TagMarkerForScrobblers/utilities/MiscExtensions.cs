using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TagMarkerForScrobblers
{
    public static class MiscExtensions
    {
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
    }
}
