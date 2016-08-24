using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagLib;
using TagMarkerForScrobblers.model;

namespace TagMarkerForScrobblers
{
    class Program
    {
        public static List<string> ErrorMessages = new List<string>();
        public static Settings Config { get; set; }

        static void Main(string[] args)
        {
            try
            {
                LoadConfig();

                BackupScrobblerFile(Config.FilePath_MasterScrobbler, Config.DirectoryPath_ScrobblerBackups);

                SaveNewScrobbleDataFromMP3Player();

                List<string> audioFilePaths = Directory
                    .GetFiles(Config.DirectoryPath_AudioFiles, "*.*", SearchOption.AllDirectories)
                    .ToList();

                UpdateTags(audioFilePaths);

                var scrobblerData = ScrobblerParseHelper.ParseScrobblerData(Config.FilePath_MasterScrobbler);

                var aggregatedScrobblerStats = ScrobblerAggregationHelper.AggregateScrobblerData(scrobblerData);

                // Don't bother resetting stats for now.
                //TODO: Reset stats for all files that are not in the scrobble list but are in the directory.
                //ResetStats(audioFilePaths);


                AddStatsToAudioFiles(audioFilePaths, aggregatedScrobblerStats);
                Console.WriteLine("Finished.");

            }
            catch (Exception e)
            {
                ShowError(e.ToString());
            }

            if (ErrorMessages.Count > 0)
            {
                PrintMessage("Errors encountered: ");
                PrintMessage(string.Join(Environment.NewLine, ErrorMessages));
            }

            Console.ReadLine();
        }

        private static void SaveNewScrobbleDataFromMP3Player()
        {

            var mp3PlayerScrobblerFilePath = GetMp3PlayerScrobblerFilePath();

            // Load mp3 player scrobble file contents
            var mp3PlayerScrobblerFileContents = System.IO.File.ReadAllText(mp3PlayerScrobblerFilePath);

            // Load master scrobble file contents
            var masterScrobbleFileContents = System.IO.File.ReadAllText(Config.FilePath_MasterScrobbler);

            // Append MP3 Player scrobbler file contents to master scrobbler file and save
            masterScrobbleFileContents += mp3PlayerScrobblerFileContents;
            System.IO.File.WriteAllText(path: Config.FilePath_MasterScrobbler, contents: masterScrobbleFileContents);

            // Truncate mp3 player scrobbler file
            System.IO.File.WriteAllText(path: mp3PlayerScrobblerFilePath, contents: "");
        }

        private static string GetMp3PlayerScrobblerFilePath()
        {
            var driveInfoList = DriveInfo.GetDrives();

            DirectoryInfo driveDirectory = null;
            foreach (var driveInfo in driveInfoList)
            {
                try
                {
                    if (driveInfo.VolumeLabel.ToLower() == Config.VolumeLabel_Mp3Player.ToLower())
                    {
                        driveDirectory = driveInfo.RootDirectory;
                        break;
                    }
                }
                catch (Exception e)
                {
                    ShowError(e.Message);

                }
            }

            if (driveDirectory == null)
            {
                throw new DriveNotFoundException("Could not find drive with volume label " + Config.VolumeLabel_Mp3Player);
            }

            return Path.Combine(driveDirectory.FullName, Config.RelativeFilePath_Mp3PlayerScrobbler);
        }

        private static void ShowError(string message)
        {
            PrintMessage("Error: " + message);
            ErrorMessages.Add(message);
        }

        private static void BackupScrobblerFile(string scrobblerFilePath, string scrobblerBackupDirectory)
        {
            string dateStr = DateTime.Now.FormatAs_yyyyMMdd();
            string scrobblerFileName = Path.GetFileName(scrobblerFilePath);

            var resultFilename = Path.Combine(scrobblerBackupDirectory, dateStr + "-" + scrobblerFileName);

            System.IO.File.Copy(scrobblerFilePath, resultFilename, true);
        }

        private static void ResetStats(List<string> audioFilePaths)
        {
            foreach (var audioFilePath in audioFilePaths)
            {
                TagLib.File taglibFile = TagLib.File.Create(audioFilePath);

                taglibFile.SetCustomValue(TagCustomKey.LastPlayed, "");
                taglibFile.SetCustomValue(TagCustomKey.TimesFinished, "");
                taglibFile.SetCustomValue(TagCustomKey.TimesSkipped, "");
                taglibFile.SetCustomValue(TagCustomKey.WeightedRating, "");

                taglibFile.Save();

                // impressions = times skipped + times finished

                PrintMessage(Path.GetFileNameWithoutExtension(audioFilePath) + ": Reset stats");
            }
        }

        private static void PrintMessage(string msg)
        {
            // Remove BELL characters (cause beep)
            msg = msg.Replace("•", "");

            Debug.WriteLine(msg);
            Console.Out.WriteLine(msg);
        }

        private static void AddStatsToAudioFiles(List<string> audioFilePaths, List<AudioFileStatInfo> audioFileStatList)
        {
            var audioFileNames = audioFilePaths.Select(afp => Path.GetFileName(afp)).ToList();

            foreach (var statInfo in audioFileStatList)
            {
                PrintMessage("");

                var matchingAudioFileNameSearch = audioFileNames
                    .Where(afn => afn.StartsWith(statInfo.PartialFileName))
                    .ToList();

                if (matchingAudioFileNameSearch.Count == 0)
                {
                    PrintMessage("Could not find file named " + statInfo.PartialFileName);
                    continue;
                }

                var fullFilePath = audioFilePaths.Where(afp => afp.EndsWith(matchingAudioFileNameSearch.First())).First();

                TagLib.File taglibFile = TagLib.File.Create(fullFilePath);

                taglibFile.SetCustomValue(TagCustomKey.WeightedRating, statInfo.WeightedRating);
                taglibFile.SetCustomValue(TagCustomKey.TimesSkipped, statInfo.TimesSkipped);
                taglibFile.SetCustomValue(TagCustomKey.TimesFinished, statInfo.TimesFinished);
                taglibFile.SetCustomValue(TagCustomKey.LastPlayed, statInfo.LastPlayed.ToString("u"));

                taglibFile.Save();


                string changeSummary = Path.GetFileNameWithoutExtension(statInfo.PartialFileName) + ": " + Environment.NewLine;

                changeSummary += "Set Weighted Rating = " + taglibFile.GetCustomValue(TagCustomKey.WeightedRating) + Environment.NewLine;
                changeSummary += "Set Times Skipped = " + taglibFile.GetCustomValue(TagCustomKey.TimesSkipped) + Environment.NewLine;
                changeSummary += "Set Times Finished = " + taglibFile.GetCustomValue(TagCustomKey.TimesFinished) + Environment.NewLine;
                changeSummary += "Set Last Played = " + taglibFile.GetCustomValue(TagCustomKey.LastPlayed) + Environment.NewLine;

                PrintMessage(changeSummary);
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
                    PrintMessage(e.ToString());
                }
            }

        }

        private static void AddFilenameTagMarkerIfNotAlreadyAdded(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            TagLib.File taglibFile = TagLib.File.Create(filePath);

            if (HasFilenameTagMarker(taglibFile.Tag))
            {
                return;
            }

            AddFilenameTagMarker(taglibFile.Tag, fileName);
            taglibFile.Save();

            PrintMessage("Added tag marker to " + fileName);
            PrintMessage("");

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

                PrintMessage("Added artist name '" + artistName + "' to " + fileName);
                PrintMessage("");
            }

            if (!HasTrackTitle(taglibFile.Tag))
            {
                string trackTitle = GetTrackTitleFromFileName(fileName);
                taglibFile.Tag.Title = trackTitle;
                taglibFile.Save();

                PrintMessage("Added track title '" + trackTitle + "' to " + fileName);
                PrintMessage("");
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
