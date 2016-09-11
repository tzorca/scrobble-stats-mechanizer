using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrobbleStatsMechanizerCommon
{
    public class ScrobbleStatsForFile
    {
        public ScrobbleStatsForFile(string title, string artist, string album, uint timesStarted, uint timesSkipped, uint timesFinished, DateTime lastPlayed, DateTime firstPlayed)
        {
            this.Title = title;
            this.Artist = artist;
            this.Album = album;
            this.TimesStarted = timesStarted;
            this.TimesSkipped = timesSkipped;
            this.TimesFinished = timesFinished;
            this.LastPlayed = lastPlayed;
            this.FirstPlayed = firstPlayed;
        }

        public string Title { get; private set; }
        public string Artist { get; private set; }
        public string Album { get; private set; }
        public uint TimesStarted { get; private set; }
        public uint TimesFinished { get; private set; }
        public uint TimesSkipped { get; private set; }
        public DateTime LastPlayed { get; private set; }
        public DateTime FirstPlayed { get; private set; }
        public string WeightedRating { get; set; }

        public override string ToString()
        {
            return Artist + " - " + Title;
        }
    }
}
