using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrobbleStatsMechanizerCommon
{
    public class ScrobbleStatsToTagLibFileSearchResult
    {
        public double MatchPercent { get; set; }
        public List<TagLib.File> MatchingFiles { get; set; }
    }
}
