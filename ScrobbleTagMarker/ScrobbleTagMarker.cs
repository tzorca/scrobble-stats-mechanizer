using ScrobbleStatsMechanizerCommon;
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
        /// If a file doesn't have artist and title tags set, sets them by deriving the values from the filename. If a file already has artist and title tags set, this function will do nothing.
        /// </summary>
        /// <param name="tagLibFile">The tag lib file for which to initialize tags</param>
        /// <returns>True if tags were updated and saved</returns>
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
        /// Copies the file at masterScrobblerFilePath to the masterScrobbleBackupDirectoryPath directory, including a date string to separate backups from different dates.
        /// </summary>
        /// <param name="masterScrobbleFilePath">The complete path to the master scrobbler file</param>
        /// <param name="masterScrobbleBackupDirectoryPath">The complete path to the scrobbler backup directory</param>
        public void BackupScrobblerFile(string masterScrobbleFilePath, string masterScrobbleBackupDirectoryPath)
        {
            string dateStr = DateTime.Now.FormatAs_yyyyMMdd();
            string scrobblerFileName = Path.GetFileName(masterScrobbleFilePath);

            var resultFilename = Path.Combine(masterScrobbleBackupDirectoryPath, dateStr + "-" + scrobblerFileName);

            System.IO.File.Copy(masterScrobbleFilePath, resultFilename, true);
        }

        /// <summary>
        /// Clears scrobble-related stats tag (Last Played, Times Finished, Weighted Rating, etc.) in all the specified tagLibFiles.
        /// </summary>
        /// <param name="tagLibFiles">The List of TagLib.Files for which to clear stats</param>
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
        /// Finds the best artist and title match(es) from a collection of possible tag lib files.
        /// </summary>
        /// <param name="scrobbleStatsForFile">Scrobbler stats connected to a particular artist and tag. The search key.</param>
        /// <param name="tagLibFiles">The collection of TagLib.File to search</param>
        /// <returns>Match result, containing the matches files found and the match percent.</returns>
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
        /// <param name="tagLibFile">The TagLib.File to check</param>
        /// <param name="statInfo">The ScrobbleStatsForFile to compare</param>
        /// <returns>Percentage of match strength. Will return 0 if the match has no possibility of being valid.</returns>
        internal double GetArtistAndTitleMatchStrength(TagLib.File tagLibFile, ScrobbleStatsForFile statInfo)
        {
            var performers = tagLibFile.Tag.Performers;

            var artist = statInfo.Artist;

            if (performers.Any(p => p.Contains("Two Dragons")) && artist.Contains("Two Dragons"))
            {
                Debug.WriteLine(string.Join("|", performers));
                Debug.WriteLine(artist);

                var c = 1;
            }

            double titleMatchStrength = 0;
            if (!tagLibFile.Tag.Title.StartsWith(statInfo.Title))
            {
                return 0;
            }
            else
            {
                titleMatchStrength = (statInfo.Title.Length / (double)tagLibFile.Tag.Title.Length);
            }

            double artistMatchStrength = 0;
            var fileTagFirstArtist = tagLibFile.Tag.Performers.First();
            if (fileTagFirstArtist.StartsWith(statInfo.Artist))
            {
                artistMatchStrength = (statInfo.Artist.Length / (double)fileTagFirstArtist.Length);
            }
            else if (statInfo.Artist.StartsWith(fileTagFirstArtist))
            {
                artistMatchStrength = (fileTagFirstArtist.Length / (double)statInfo.Artist.Length);
            }
            else
            {
                return 0;
            }

            return (titleMatchStrength + artistMatchStrength) / 2d;
        }

        /// <summary>
        /// Returns true if one or more stats were updated in the file
        /// </summary>
        /// <param name="taglibFile">The TagLib.File to be updated with stats</param>
        /// <param name="newStats">The ScrobbleStatsForFile to set the stats to</param>
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
        /// Loads a TagLib.File for each file in the directory at audioCollectionDirectoryPath
        /// </summary>
        /// <param name="audioCollectionDirectoryPath">The complete path to the audio collection directory</param>
        /// <returns>A list of TagLibFileLoadResult. Each result contains the loaded TagLib.File (if loaded successfully), an error (if applicable), and the name of the original file path.</returns>
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
