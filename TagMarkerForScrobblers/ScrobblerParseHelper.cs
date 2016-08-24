using System;
using System.IO;
using System.Linq;
using TagMarkerForScrobblers.model;
using System.Collections.Generic;

namespace TagMarkerForScrobblers
{
    internal class ScrobblerParseHelper
    {
        internal static List<ScrobbleRecord> ParseScrobblerData(string scrobblerFilePath)
        {
            string scrobblerData = File.ReadAllText(scrobblerFilePath);

            var scrobblerDataLines = scrobblerData.SplitIntoLines();

            // Keep only unique lines
            scrobblerDataLines = scrobblerDataLines.Distinct().ToList();

            var linesWithIdentifiableTrack = scrobblerDataLines
                .Where(l => l.Contains("[["));

            List<ScrobbleRecord> scrobbleRecords = linesWithIdentifiableTrack
                .Select(l => ScrobbleRecord.FromLine(l))
                .ToList();


            return scrobbleRecords;
        }
    }
}