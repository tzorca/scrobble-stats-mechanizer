using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TagMarkerForScrobblers.model
{
    public class AudioFileStatInfo
    {
        public AudioFileStatInfo(string partialFileName, uint timesStarted, uint timesSkipped, uint timesFinished, DateTime lastPlayed, DateTime firstPlayed)
        {
            this.PartialFileName = partialFileName;
            this.TimesStarted = timesStarted;
            this.TimesSkipped = timesSkipped;
            this.TimesFinished = timesFinished;
            this.LastPlayed = lastPlayed;
            this.FirstPlayed = firstPlayed;
        }

        public string PartialFileName { get; private set; }
        public uint TimesStarted { get; private set; }
        public uint TimesFinished { get; private set; }
        public uint TimesSkipped { get; private set; }
        public DateTime LastPlayed { get; private set; }
        public DateTime FirstPlayed { get; private set; }
        public string WeightedRating { get; internal set; }
    }
}
