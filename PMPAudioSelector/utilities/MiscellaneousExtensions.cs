using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMPAudioSelector
{
    public static class MiscellaneousExtensions
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
    }
}
