using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TagMarkerForScrobblers.model
{
    public class ScrobbleRecord
    {
        internal static ScrobbleRecord FromLine(string inputLine)
        {
            var scrobbleRecord = new ScrobbleRecord();
            var components = inputLine.Split('\t');

            scrobbleRecord.Author = components[0];
            scrobbleRecord.Album = components[1];
            scrobbleRecord.Title = components[2];
            scrobbleRecord.UnidentifiedField1 = components[3];
            scrobbleRecord.TrackLength = components[4];
            scrobbleRecord.Skipped = (components[5] ?? "") == "S";
            scrobbleRecord.TimeStamp = components[6];

            return scrobbleRecord;
        }

        public string Author { get; private set; }
        public string Album { get; private set; }
        public string Title { get; private set; }
        public string TimeStamp { get; private set; }
        public string TrackLength { get; private set; }
        public string UnidentifiedField1 { get; private set; }
        public bool Skipped { get; private set; }



        public DateTime GetDateTime()
        {
            return DateUtilities.UnixTimeStampToDateTime(Double.Parse(TimeStamp));
        }

        public string ArtistTitleGrouping()
        {
            return Album + " - " + Author;
        }

    }
}
