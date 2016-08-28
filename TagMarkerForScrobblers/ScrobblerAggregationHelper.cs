using System;
using System.Linq;
using System.Collections.Generic;
using TagMarkerForScrobblers.model;

namespace TagMarkerForScrobblers
{
    internal class ScrobblerAggregationHelper
    {
        internal static List<AudioFileStatInfo> AggregateScrobblerData(List<ScrobbleRecord> scrobblerRecords)
        {
            var recordsByFilename = scrobblerRecords.GroupBy(sr => sr.GetFilenameFromAlbum());

            var audioFileStatInfoList = new List<AudioFileStatInfo>();
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

                var audioFileStatInfo = new AudioFileStatInfo
                (
                    partialFileName: fileHistory.Key,
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