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

                var audioFileStatInfo = new AudioFileStatInfo(fileHistory.Key, timesStarted, timesSkipped, timesFinished, lastPlayed);

                audioFileStatInfoList.Add(audioFileStatInfo);
            }

            return audioFileStatInfoList;
        }
    }
}