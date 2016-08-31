using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagLib;

namespace ScrobbleStatsMechanizer
{
    public class ScrobbleTagMarker
    {
        /// <summary>
        /// TODO: Document this function.
        /// </summary>
        /// <param name="scrobblerRecords"></param>
        /// <returns></returns>
        public List<ScrobbleStatsForFile> AggregateScrobblerData(List<ScrobbleRecord> scrobblerRecords)
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

        /// <summary>
        /// TODO: Document this function.
        /// </summary>
        /// <param name="masterScrobbleFilePath"></param>
        /// <returns></returns>
        public List<ScrobbleRecord> ParseScrobblerData(string masterScrobbleFilePath)
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
        /// TODO: Document this function.
        /// </summary>
        /// <param name="tagLibFile"></param>
        /// <returns>True if tags were saved</returns>
        public bool InitializeArtistAndTitleTags(TagLib.File tagLibFile)
        {

            string filePath = tagLibFile.Name;
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);

            bool saveNeeded = false;

            if (!tagLibFile.Tag.HasArtist())
            {
                SetArtistFromFileName(tagLibFile, fileNameWithoutExt);
                saveNeeded = true;
            }

            if (!tagLibFile.Tag.HasTrackTitle())
            {
                SetTitleFromFileName(tagLibFile, fileNameWithoutExt);
                saveNeeded = true;
            }

            if (saveNeeded)
            {
                tagLibFile.Save();
                return true;
            }

            return false;
        }


        /// <summary>
        /// Appends contents from PMP scrobbler file to a master scrobbler file.
        /// Optionally truncates the PMP scrobbler file after appending its contents to master scrobbler file.
        /// </summary>
        /// <param name="pmpScrobbleFilePath">The complete path to the PMP scrobbler file</param>
        /// <param name="masterScrobbleFilePath">The complete path to the master scrobbler file</param>
        /// <param name="truncateScrobblerFile">If true, the contents of the PMP scrobbler file will be set to a blank string.</param>
        public void SaveNewScrobbleDataFromMP3Player(string pmpScrobbleFilePath, string masterScrobbleFilePath, bool truncateScrobblerFile = false)
        {


            // Load mp3 player scrobble file contents
            var mp3PlayerScrobblerFileContents = System.IO.File.ReadAllText(pmpScrobbleFilePath);

            // Load master scrobble file contents
            var masterScrobbleFileContents = System.IO.File.ReadAllText(masterScrobbleFilePath);

            // Append PMP scrobbler file contents to master scrobbler file and save
            masterScrobbleFileContents += mp3PlayerScrobblerFileContents;
            System.IO.File.WriteAllText(path: masterScrobbleFilePath, contents: masterScrobbleFileContents);

            // Truncate PMP scrobbler file
            if (truncateScrobblerFile)
            {
                System.IO.File.WriteAllText(path: pmpScrobbleFilePath, contents: "");
            }
        }

        /// <summary>
        /// TODO: Document this function.
        /// </summary>
        /// <param name="masterScrobbleFilePath"></param>
        /// <param name="masterScrobbleBackupDirectoryPath"></param>
        public void BackupScrobblerFile(string masterScrobbleFilePath, string masterScrobbleBackupDirectoryPath)
        {
            string dateStr = DateTime.Now.FormatAs_yyyyMMdd();
            string scrobblerFileName = Path.GetFileName(masterScrobbleFilePath);

            var resultFilename = Path.Combine(masterScrobbleBackupDirectoryPath, dateStr + "-" + scrobblerFileName);

            System.IO.File.Copy(masterScrobbleFilePath, resultFilename, true);
        }

        /// <summary>
        /// TODO: Document this function.
        /// </summary>
        /// <param name="tagLibFiles"></param>
        public void ResetStats(List<TagLib.File> tagLibFiles)
        {
            foreach (var tagLibFile in tagLibFiles)
            {
                tagLibFile.SetCustomValue(TagCustomKey.LastPlayed, "");
                tagLibFile.SetCustomValue(TagCustomKey.FirstPlayed, "");
                tagLibFile.SetCustomValue(TagCustomKey.TimesFinished, "");
                tagLibFile.SetCustomValue(TagCustomKey.TimesSkipped, "");
                tagLibFile.SetCustomValue(TagCustomKey.WeightedRating, "");

                tagLibFile.Save();

                // impressions = times skipped + times finished
            }
        }


        /// <summary>
        /// TODO: Document this function.
        /// </summary>
        /// <param name="scrobbleStatsForFile"></param>
        /// <param name="tagLibFiles"></param>
        /// <returns>Match result</returns>
        public ScrobbleStatsToTagLibFileSearchResult FindMatchingTagLibFile(ScrobbleStatsForFile scrobbleStatsForFile, List<TagLib.File> tagLibFiles)
        {
            var tagLibFilesByMatchPercentage = tagLibFiles
                .GroupBy(tagLibFile => GetArtistAndTitleMatchStrength(tagLibFile, scrobbleStatsForFile))
                .OrderByDescending(group => group.Key)
                .ToList();

            var bestMatchGroup = tagLibFilesByMatchPercentage.First();

            return new ScrobbleStatsToTagLibFileSearchResult()
            {
                MatchingFiles = bestMatchGroup.ToList(),
                MatchPercent = bestMatchGroup.Key
            };


        }

        /// <summary>
        /// Returns the percentage of characters in the file tags that match characters in the stats.
        /// Requires at least a StartsWith match to return a non-zero result.
        /// </summary>
        /// <param name="tagLibFile"></param>
        /// <param name="statInfo"></param>
        /// <returns>Percentage of match strength. Will return 0 if the match has no possibility of being valid.</returns>
        internal double GetArtistAndTitleMatchStrength(TagLib.File tagLibFile, ScrobbleStatsForFile statInfo)
        {
            if (!tagLibFile.Tag.Title.StartsWith(statInfo.Title))
            {
                return 0;
            }

            var fileTagFirstArtist = tagLibFile.Tag.Performers.First();
            if (!fileTagFirstArtist.StartsWith(statInfo.Artist))
            {
                return 0;
            }

            double titleMatchStrength = (statInfo.Title.Length / (double)tagLibFile.Tag.Title.Length);
            double artistMatchStrength = (statInfo.Artist.Length / (double)fileTagFirstArtist.Length);
            return (titleMatchStrength + artistMatchStrength) / 2d;
        }

        /// <summary>
        /// Returns true if one or more stats were updated in the file
        /// </summary>
        /// <param name="taglibFile"></param>
        /// <param name="newStats"></param>
        /// <returns>True if one or more stats were updated in the file</returns>
        public bool UpdateScrobblerStatsInTagLibFile(TagLib.File taglibFile, ScrobbleStatsForFile newStats)
        {
            bool statsChanged = false;
            statsChanged = taglibFile.SetCustomValue(TagCustomKey.WeightedRating, newStats.WeightedRating) || statsChanged;
            statsChanged = taglibFile.SetCustomValue(TagCustomKey.TimesSkipped, newStats.TimesSkipped) || statsChanged;
            statsChanged = taglibFile.SetCustomValue(TagCustomKey.TimesFinished, newStats.TimesFinished) || statsChanged;
            statsChanged = taglibFile.SetCustomValue(TagCustomKey.LastPlayed, newStats.LastPlayed.ToString("u")) || statsChanged;
            statsChanged = taglibFile.SetCustomValue(TagCustomKey.FirstPlayed, newStats.FirstPlayed.ToString("u")) || statsChanged;

            if (statsChanged)
            {
                taglibFile.Save();
                return true;
            }

            return false;
        }

        /// <summary>
        /// TODO: Document this function.
        /// </summary>
        /// <param name="audioCollectionDirectoryPath"></param>
        /// <returns></returns>
        public List<TagLibFileLoadResult> GetTagLibFiles(string audioCollectionDirectoryPath)
        {
            var audioFilePaths = Directory
                .GetFiles(audioCollectionDirectoryPath, "*.*", SearchOption.AllDirectories)
                .ToList();

            var tagLibFileLoadResults = new List<TagLibFileLoadResult>();
            foreach (var filePath in audioFilePaths)
            {
                var tagLibFileLoadResult = new TagLibFileLoadResult(filePath);

                string fileNameWithoutExt = "";
                try
                {
                    fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                    TagLib.File tagLibFile = TagLib.File.Create(filePath);

                    tagLibFileLoadResult.TagLibFile = tagLibFile;
                }
                catch (Exception e)
                {
                    tagLibFileLoadResult.Error = e;

                }

                tagLibFileLoadResults.Add(tagLibFileLoadResult);
            }

            return tagLibFileLoadResults;
        }

        private void SetArtistFromFileName(TagLib.File taglibFile, string fileNameWithoutExt)
        {
            string artistName = DetermineArtistNameFromFileName(fileNameWithoutExt);
            taglibFile.Tag.Performers = new string[] { artistName };
        }
        private void SetTitleFromFileName(TagLib.File taglibFile, string fileNameWithoutExt)
        {
            string trackTitle = DetermineTrackTitleFromFileName(fileNameWithoutExt);
            taglibFile.Tag.Title = trackTitle;
        }


        private string DetermineArtistNameFromFileName(string fileNameWithoutExtension)
        {
            if (String.IsNullOrEmpty(fileNameWithoutExtension))
            {
                return "Unknown Artist";
            }

            if (fileNameWithoutExtension.Contains(" - "))
            {
                return fileNameWithoutExtension
                    .Split(new string[] { " - " }, StringSplitOptions.RemoveEmptyEntries)
                    .First();
            }
            else
            {
                return "Unknown Artist";
            }
        }


        private string DetermineTrackTitleFromFileName(string fileNameWithoutExtension)
        {
            if (String.IsNullOrEmpty(fileNameWithoutExtension))
            {
                return "Unknown Title";
            }

            if (fileNameWithoutExtension.Contains("-"))
            {
                return string.Join(" - ", fileNameWithoutExtension
                    .Split(new string[] { " - " }, StringSplitOptions.RemoveEmptyEntries)
                    .Skip(1));
            }
            else
            {
                return fileNameWithoutExtension;
            }
        }

    }
}
