using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MyExtensions;
using System.Text;
using System.Threading.Tasks;
using TagLib;
using TagMarkerForScrobblers.model;

namespace TagMarkerForScrobblers
{
    class Program
    {
        public static Settings Config { get; set; }

        static void Main(string[] args)
        {
            LoadConfig();

            List<string> audioFilePaths = Directory
                .GetFiles(Config.AudioFileDirectory, "*.*", SearchOption.AllDirectories)
                .ToList();


            UpdateTags(audioFilePaths);

            var scrobblerData = ScrobblerParseHelper.ParseScrobblerData(Config.ScrobblerFilePath);

            var aggregatedScrobblerStats = ScrobblerAggregationHelper.AggregateScrobblerData(scrobblerData);


            AddStatsToAudioFiles(audioFilePaths, aggregatedScrobblerStats);


            BackupScrobblerFile(Config.ScrobblerFilePath, Config.ScrobblerBackupDirectory);
        }

        private static void BackupScrobblerFile(string scrobblerFilePath, string scrobblerBackupDirectory)
        {
            string dateStr = DateTime.Now.FormatAs_yyyyMMdd();
            string scrobblerFileName = Path.GetFileName(scrobblerFilePath);

            var resultFilename = Path.Combine(scrobblerBackupDirectory, dateStr + "-" + scrobblerFileName);

            System.IO.File.Copy(scrobblerFilePath, resultFilename, true);
        }

        private static void AddStatsToAudioFiles(List<string> audioFilePaths, List<AudioFileStatInfo> audioFileStatList)
        {
            var audioFileNames = audioFilePaths.Select(afp => Path.GetFileName(afp)).ToList();

            foreach (var statInfo in audioFileStatList)
            {
                Debug.WriteLine("");

                var matchingAudioFileNameSearch = audioFileNames
                    .Where(afn => afn.StartsWith(statInfo.FileName))
                    .ToList();

                if (matchingAudioFileNameSearch.Count == 0)
                {
                    Debug.WriteLine("Could not find file named " + statInfo.FileName);
                    continue;
                }

                var fullFilePath = audioFilePaths.Where(afp => afp.EndsWith(matchingAudioFileNameSearch.First())).First();

                TagLib.File taglibFile = TagLib.File.Create(fullFilePath);

                taglibFile.Tag.DiscCount = statInfo.TimesSkipped;
                taglibFile.Tag.Disc = statInfo.TimesFinished;
                //SetCustomTag(id3v2_tag, "last_played", statInfo.LastPlayed.FormatAs_yyyyMMdd());

                taglibFile.Save();

                Debug.WriteLine("Set Disc Count to " + statInfo.TimesSkipped.ToString());
                Debug.WriteLine("Set Disc to " + statInfo.TimesFinished.ToString());

                //if (statInfo.TimesSkipped > 2 || statInfo.TimesFinished > 2)
                //{
                //    var dc = 1; 
                //}
                Debug.WriteLine("Updated stats for " + statInfo.FileName);
            }
        }

        private static void UpdateTags(List<string> filePaths)
        {
            foreach (var filePath in filePaths)
            {
                try
                {
                    AddFilenameTagMarkerIfNotAlreadyAdded(filePath);
                    SetArtistAndTrackNameIfNotSet(filePath);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }
            }

        }

        private static void AddFilenameTagMarkerIfNotAlreadyAdded(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            TagLib.File taglibFile = TagLib.File.Create(filePath);

            if (HasFilenameTagMarker(taglibFile.Tag))
            {
                Debug.WriteLine("Skipped " + fileName);
                Debug.WriteLine("Already has tag marker.");
                Debug.WriteLine("");
                return;
            }
            else
            {
                AddFilenameTagMarker(taglibFile.Tag, fileName);
                taglibFile.Save();

                Debug.WriteLine("Added tag marker to " + fileName);
                Debug.WriteLine("");
            }

        }


        private static void SetArtistAndTrackNameIfNotSet(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            TagLib.File taglibFile = TagLib.File.Create(filePath);

            if (!HasArtist(taglibFile.Tag))
            {
                string artistName = GetArtistNameFromFileName(fileName);
                taglibFile.Tag.Performers = new string[] { artistName };
                taglibFile.Save();

                Debug.WriteLine("Added artist name '" + artistName + "' to " + fileName);
                Debug.WriteLine("");
            }

            if (!HasTrackTitle(taglibFile.Tag))
            {
                string trackTitle = GetTrackTitleFromFileName(fileName);
                taglibFile.Tag.Title = trackTitle;
                taglibFile.Save();

                Debug.WriteLine("Added track title '" + trackTitle + "' to " + fileName);
                Debug.WriteLine("");
            }


        }
        private static string GetArtistNameFromFileName(string fileNameWithoutExtension)
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


        private static string GetTrackTitleFromFileName(string fileNameWithoutExtension)
        {
            if (String.IsNullOrEmpty(fileNameWithoutExtension))
            {
                return "Unknown Title";
            }

            if (fileNameWithoutExtension.Contains("-"))
            {
                return string.Join("", fileNameWithoutExtension
                    .Split(new string[] { " - " }, StringSplitOptions.RemoveEmptyEntries)
                    .Skip(1));
            }
            else
            {
                return fileNameWithoutExtension;
            }
        }

        private static bool HasArtist(Tag tag)
        {
            if (tag.Performers == null)
            {
                return false;
            }

            if (tag.Performers.Length == 0)
            {
                return false;
            }

            string firstArtist = tag.Performers.First();

            if (String.IsNullOrEmpty(firstArtist))
            {
                return false;
            }

            if (firstArtist == "Unidentified" || firstArtist == "Unspecified")
            {
                return false;
            }

            return true;
        }

        private static bool HasTrackTitle(Tag tag)
        {
            if (String.IsNullOrEmpty(tag.Title))
            {
                return false;
            }

            if (tag.Title == "Unidentified" || tag.Title == "Unspecified")
            {
                return false;
            }

            return true;
        }


        private static void AddFilenameTagMarker(Tag tag, string fileName)
        {
            if (tag.Album == null)
            {
                tag.Album = "";
            }

            tag.Album += " [[" + fileName + "]]";
        }

        private static bool HasFilenameTagMarker(Tag tag)
        {
            if (tag.Album == null)
            {
                return false;
            }

            return tag.Album.Contains(" [[") && tag.Album.EndsWith("]]");
        }

        private static void LoadConfig()
        {
            string configText = System.IO.File.ReadAllText("tag-marker.js");
            Config = JsonConvert.DeserializeObject<Settings>(configText);

        }
    }
}
