﻿using Newtonsoft.Json;
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

                var tagFiles = GetTagFiles(audioFilePaths);

                InitializeTagValues(tagFiles);

                PrintMessage("Parsing scrobbler data...");
                var scrobblerData = ScrobblerParseHelper.ParseScrobblerData(Config.FilePath_MasterScrobbler);

                PrintMessage("Aggregating scrobbler data...");
                var aggregatedScrobblerStats = ScrobblerAggregationHelper.AggregateScrobblerData(scrobblerData);

                // Don't bother resetting stats for now.
                //TODO: Reset stats for all files that are not in the scrobble list but are in the directory.
                //ResetStats(audioFilePaths);


                UpdateScrobblerStatsInTagLibFiles(tagFiles, aggregatedScrobblerStats);
                Console.WriteLine("Finished.");

            }
            catch (Exception e)
            {
                PrintError(e.ToString());
            }

            if (ErrorMessages.Count > 0)
            {
                PrintMessage("Errors encountered: ");
                PrintMessage(string.Join(Environment.NewLine, ErrorMessages));
            }

            Console.ReadLine();
        }

        private static void InitializeTagValues(List<TagLib.File> tagLibFiles)
        {
            PrintMessage("Initializing tag values...");
            foreach (var tagLibFile in tagLibFiles)
            {
                try
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
                        PrintMessage("Initializing tag values for " + fileNameWithoutExt);
                        tagLibFile.Save();
                        PrintMessage("");
                    }
                }
                catch (Exception e)
                {
                    PrintError("Error initializing tag values: " + e.Message);
                    PrintMessage("");
                }
            }
        }

        private static void SaveNewScrobbleDataFromMP3Player()
        {
            PrintMessage("Saving new scrobble data from MP3 player...");

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
            PrintMessage("Finding MP3 player scrobbler file path...");
            var driveInfoList = DriveInfo.GetDrives();

            DirectoryInfo driveDirectory = null;
            foreach (var driveInfo in driveInfoList)
            {
                try
                {
                    if (!driveInfo.IsReady)
                    {
                        PrintMessage(driveInfo.Name + " is not ready.");
                        continue;
                    }

                    if (driveInfo.VolumeLabel.ToLower() == Config.VolumeLabel_Mp3Player.ToLower())
                    {
                        driveDirectory = driveInfo.RootDirectory;
                        break;
                    }
                }
                catch (Exception e)
                {
                    PrintError(e.Message);
                }
            }

            if (driveDirectory == null)
            {
                throw new DriveNotFoundException("Could not find drive with volume label " + Config.VolumeLabel_Mp3Player);
            }

            string resultPath = Path.Combine(driveDirectory.FullName, Config.RelativeFilePath_Mp3PlayerScrobbler);
            PrintMessage("Scrobble file path found.");
            return resultPath;
        }

        private static void PrintError(string message)
        {
            PrintMessage("Error: " + message);
            ErrorMessages.Add(message);
        }

        private static void BackupScrobblerFile(string scrobblerFilePath, string scrobblerBackupDirectory)
        {
            PrintMessage("Backing up current scrobbler file...");

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
                taglibFile.SetCustomValue(TagCustomKey.FirstPlayed, "");
                taglibFile.SetCustomValue(TagCustomKey.TimesFinished, "");
                taglibFile.SetCustomValue(TagCustomKey.TimesSkipped, "");
                taglibFile.SetCustomValue(TagCustomKey.WeightedRating, "");

                taglibFile.Save();

                // impressions = times skipped + times finished

                PrintMessage("Reset stats for " + Path.GetFileNameWithoutExtension(audioFilePath));
            }
        }

        private static void PrintMessage(string msg)
        {
            // Remove BELL characters (cause beep)
            msg = msg.Replace("•", "");

            Debug.WriteLine(msg);
            Console.Out.WriteLine(msg);
        }

        private static void UpdateScrobblerStatsInTagLibFiles(List<TagLib.File> tagLibFiles, List<AudioFileStatInfo> audioFileStatList)
        {
            PrintMessage("Updating scrobbler stats in audio files...");

            foreach (var statInfo in audioFileStatList)
            {
                try
                {
                    var tagLibFilesByMatchPercentage = tagLibFiles
                        .GroupBy(tagLibFile => GetArtistAndTitleMatchStrength(tagLibFile, statInfo))
                        .OrderByDescending(group => group.Key)
                        .ToList();

                    var bestMatchGroup = tagLibFilesByMatchPercentage.First();

                    if (bestMatchGroup.Key == 0)
                    {
                        // No good matches
                        continue;
                    }

                    TagLib.File taglibFile = bestMatchGroup.First();

                    if (bestMatchGroup.Count() > 1)
                    {
                        // Non-fatal error, but will want to warn about this
                        PrintError(String.Format("More than one {0:0.0}% match for {1}",
                            bestMatchGroup.Key * 100, statInfo.ToString())); ;
                    }

                    if (UpdateScrobblerStatsInTagLibFile(taglibFile, statInfo))
                    {
                        taglibFile.Save();
                    }
                }
                catch (Exception e)
                {
                    PrintError("Could not update stats for " + statInfo.ToString() + ": " + e.Message);
                }
            }
        }

        /// <summary>
        /// Returns the percentage of characters in the file tags that match characters in the stats.
        /// Requires at least a StartsWith match to return a non-zero result.
        /// </summary>
        /// <param name="tagLibFile"></param>
        /// <param name="statInfo"></param>
        /// <returns>Percentage of match strength. Will return 0 if the match has no possibility of being valid.</returns>
        private static double GetArtistAndTitleMatchStrength(TagLib.File tagLibFile, AudioFileStatInfo statInfo)
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
        /// Returns true if one or more stats were changed
        /// </summary>
        /// <param name="taglibFile"></param>
        /// <param name="newStatInfo"></param>
        /// <returns>True if one or more stats were changed</returns>
        private static bool UpdateScrobblerStatsInTagLibFile(TagLib.File taglibFile, AudioFileStatInfo newStatInfo)
        {
            bool statsChanged = false;
            statsChanged = taglibFile.SetCustomValue(TagCustomKey.WeightedRating, newStatInfo.WeightedRating) || statsChanged;
            statsChanged = taglibFile.SetCustomValue(TagCustomKey.TimesSkipped, newStatInfo.TimesSkipped) || statsChanged;
            statsChanged = taglibFile.SetCustomValue(TagCustomKey.TimesFinished, newStatInfo.TimesFinished) || statsChanged;
            statsChanged = taglibFile.SetCustomValue(TagCustomKey.LastPlayed, newStatInfo.LastPlayed.ToString("u")) || statsChanged;
            statsChanged = taglibFile.SetCustomValue(TagCustomKey.FirstPlayed, newStatInfo.FirstPlayed.ToString("u")) || statsChanged;

            return statsChanged;
        }

        private static List<TagLib.File> GetTagFiles(List<string> filePaths)
        {
            PrintMessage("Loading tags from audio files...");

            var tagLibFiles = new List<TagLib.File>();
            foreach (var filePath in filePaths)
            {
                string fileNameWithoutExt = "";
                try
                {
                    fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                    TagLib.File tagLibFile = TagLib.File.Create(filePath);
                    tagLibFiles.Add(tagLibFile);
                }
                catch (Exception e)
                {
                    PrintError("Error loading tags for " + fileNameWithoutExt + ": " + e.ToString());
                }
            }

            return tagLibFiles;
        }

        private static void SetArtistFromFileName(TagLib.File taglibFile, string fileNameWithoutExt)
        {
            string artistName = GetArtistNameFromFileName(fileNameWithoutExt);
            taglibFile.Tag.Performers = new string[] { artistName };

            PrintMessage("Set artist to " + artistName);
        }
        private static void SetTitleFromFileName(TagLib.File taglibFile, string fileNameWithoutExt)
        {
            string trackTitle = DetermineTrackTitleFromFileName(fileNameWithoutExt);
            taglibFile.Tag.Title = trackTitle;

            PrintMessage("Set title to " + trackTitle);
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


        private static string DetermineTrackTitleFromFileName(string fileNameWithoutExtension)
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

        private static void LoadConfig()
        {
            PrintMessage("Reading config file...");
            string configText = System.IO.File.ReadAllText("tag-marker.js");
            Config = JsonConvert.DeserializeObject<Settings>(configText);

        }
    }
}
