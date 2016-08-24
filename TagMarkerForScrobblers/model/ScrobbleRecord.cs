using MyUtils;
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
            scrobbleRecord.UnidentifiedNumber = components[3];
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
        public string UnidentifiedNumber { get; private set; }
        public bool Skipped { get; private set; }


        public string GetFilenameFromAlbum()
        {
            if (!FILENAME_IN_ALBUM_REGEX.IsMatch(Album))
            {
                throw new KeyNotFoundException("No filename was found in the album name.");
            }

            var regexMatch = FILENAME_IN_ALBUM_REGEX.Match(Album).Value;

            return regexMatch.Substring(2, regexMatch.Length - 4);
        }
        private static readonly Regex FILENAME_IN_ALBUM_REGEX = new Regex(@"\[\[.*((\]\])|($))");

        public DateTime GetDateTime()
        {
            return DateUtils.UnixTimeStampToDateTime(Double.Parse(TimeStamp));
        }

    }
}
