using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TagMarkerForScrobblers.model
{
    public class AudioFileStatInfo
    {
        public AudioFileStatInfo(string fileName, uint timesStarted, uint timesSkipped, uint timesFinished, DateTime lastPlayed)
        {
            this.FileName = fileName;
            this.TimesStarted = timesStarted;
            this.TimesSkipped = timesSkipped;
            this.TimesFinished = timesFinished;
            this.LastPlayed = lastPlayed;
        }

        public string FileName { get; private set; }
        public uint TimesStarted { get; private set; }
        public uint TimesFinished { get; private set; }
        public uint TimesSkipped { get; private set; }
        public DateTime LastPlayed { get; private set; }
    }
}
