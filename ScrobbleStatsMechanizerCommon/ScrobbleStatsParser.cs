using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrobbleStatsMechanizerCommon
{
    public class ScrobbleStatsParser
    {

        /// <summary>
        /// Parses lines in a scrobble format file to make ScrobbleRecords
        /// </summary>
        /// <param name="masterScrobbleFilePath">The complete path to the master scrobbler file</param>
        /// <returns>A list of ScrobbleRecord</returns>
        public static List<ScrobbleRecord> ParseScrobblerData(string masterScrobbleFilePath)
        {
            string scrobblerData = System.IO.File.ReadAllText(masterScrobbleFilePath);

            var scrobblerDataLines = scrobblerData.SplitIntoLines()
                .Where(line => ScrobbleRecord.IsValidLine(line));

            // Keep only unique lines
            scrobblerDataLines = scrobblerDataLines.Distinct().ToList();

            var scrobbleRecords = scrobblerDataLines
                .Select(l => ScrobbleRecord.FromLine(l))
                .ToList();


            return scrobbleRecords;
        }


        /// <summary>
        /// Uses Title and Arist to combine different stat records into single combined entry for that track. A unique combination of Title and Artist will generally represent a single file.
        /// Combined entry includes statistics such as times skipped, times finihsed, weighted rating, last played, etc.
        /// </summary>
        /// <param name="scrobblerRecords">The list of ScrobblerRecords to combine into ScrobblerStatsForFiles</param>
        /// <returns>A list of scrobbler stats that are each based on a single file (Artist + Title)</returns>
        public static List<ScrobbleStatsForFile> AggregateScrobblerData(List<ScrobbleRecord> scrobblerRecords)
        {
            var recordsByFilename = scrobblerRecords.GroupBy(sr => sr.ArtistTitleGrouping());

            var audioFileStatInfoList = new List<ScrobbleStatsForFile>();
            foreach (var fileHistory in recordsByFilename)
            {
                uint timesStarted = (uint)fileHistory.Count();
                uint timesSkipped = (uint)fileHistory.Where(s => s.Skipped).Count();
                uint timesFinished = (uint)fileHistory.Where(s => !s.Skipped).Count();
                DateTime lastPlayed = fileHistory.Max(s => s.GetDateTime());
                DateTime firstPlayed = fileHistory.Min(s => s.GetDateTime());

                // Calculate weighted rating
                double weight = 50;
                double weightedRating = 1000;
                var fileHistoryMostRecentFirst = fileHistory.OrderBy(fh => fh.GetDateTime()).Reverse().ToList();
                foreach (var entry in fileHistoryMostRecentFirst)
                {
                    if (entry.Skipped)
                    {
                        weightedRating += -weight;
                    }
                    else
                    {
                        weightedRating += weight * 2;
                    }

                    weight /= 1.5;
                }

                var audioFileStatInfo = new ScrobbleStatsForFile
                (
                    title: fileHistory.First().Title,
                    artist: fileHistory.First().Author,
                    album: fileHistory.First().Author,
                    timesStarted: timesStarted,
                    timesSkipped: timesSkipped,
                    timesFinished: timesFinished,
                    lastPlayed: lastPlayed,
                    firstPlayed: firstPlayed
                );

                audioFileStatInfo.WeightedRating = ((int)weightedRating).ToString();

                audioFileStatInfoList.Add(audioFileStatInfo);
            }

            return audioFileStatInfoList;
        }

    }
}
