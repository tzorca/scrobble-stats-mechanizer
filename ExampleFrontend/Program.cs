using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrobbleStatsMechanizer.ExampleFrontend
{
    public class Program
    {
        internal static Settings Config { get; set; }

        static void Main(string[] args)
        {
            try
            {
                LoadConfig();

                var scrobblerTagMarker = new ScrobbleTagMarker();

                PrintMessage("Backing up current scrobbler file...");
                scrobblerTagMarker.BackupScrobblerFile
                (
                    masterScrobbleFilePath: Config.FilePath_MasterScrobbler,
                    masterScrobbleBackupDirectoryPath: Config.DirectoryPath_ScrobblerBackups
                );

                PrintMessage("Finding MP3 player scrobbler file path...");
                string mp3PlayerScrobbleFilePath = GetPathFromVolumeLabelAndRelativePath
                (
                    volumeLabel: Config.VolumeLabel_Mp3Player,
                    relativePath: Config.RelativeFilePath_Mp3PlayerScrobbler
                );

                PrintMessage("Saving new scrobble data from MP3 player...");
                scrobblerTagMarker.SaveNewScrobbleDataFromMP3Player
                (
                    pmpScrobbleFilePath: mp3PlayerScrobbleFilePath,
                    masterScrobbleFilePath: Config.FilePath_MasterScrobbler,
                    truncateScrobblerFile: true
                );


                PrintMessage("Loading tags from audio files...");
                var tagLibFileLoadResults = scrobblerTagMarker.GetTagLibFiles(audioCollectionDirectoryPath: Config.DirectoryPath_AudioFiles);
                foreach (var loadResult in tagLibFileLoadResults.Where(result => result.Error != null))
                {
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(loadResult.FilePath);
                    PrintError("Error loading tags for " + fileNameWithoutExt + ": " + loadResult.Error.ToString());
                }
                var tagLibFiles = tagLibFileLoadResults
                    .Select(result => result.TagLibFile)
                    .Where(tagLibFile => tagLibFile != null).ToList();


                foreach (var tagLibFile in tagLibFiles)
                {
                    string filenameWithoutExt = "";
                    try
                    {
                        filenameWithoutExt = Path.GetFileNameWithoutExtension(tagLibFile.Name);

                        var tagsSaved = scrobblerTagMarker.InitializeArtistAndTitleTags(tagLibFile);

                        if (tagsSaved)
                        {
                            PrintMessage(String.Format("Initialized artist and title for {0}",
                                filenameWithoutExt));
                        }
                    }
                    catch (Exception e)
                    {
                        PrintError(String.Format("Error initializing tag values for {0}: {1}",
                            filenameWithoutExt, e.Message));
                        PrintMessage("");
                    }
                }

                PrintMessage("Parsing scrobbler data...");
                var scrobblerData = scrobblerTagMarker.ParseScrobblerData(Config.FilePath_MasterScrobbler);

                PrintMessage("Aggregating scrobbler data...");
                var scrobblerStatsForFiles = scrobblerTagMarker.AggregateScrobblerData(scrobblerData);

                PrintMessage("Reseting tag stats...");
                scrobblerTagMarker.ResetStats(tagLibFiles);

                PrintMessage("Matching scrobbler stats with files and updating tag stats");
                foreach (var scrobbleStatsForFile in scrobblerStatsForFiles)
                {
                    try
                    {
                        var searchResult = scrobblerTagMarker.FindMatchingTagLibFile(scrobbleStatsForFile, tagLibFiles);

                        if (searchResult.MatchPercent == 0 ||
                            searchResult.MatchingFiles.Count == 0)
                        {
                            // No good matches
                            continue;
                        }

                        TagLib.File taglibFile = searchResult.MatchingFiles.First();

                        if (searchResult.MatchingFiles.Count > 1)
                        {
                            // Non-fatal error, but will want to warn about this
                            PrintError(String.Format("More than one {0:0.0}% match for {1}",
                                searchResult.MatchPercent * 100, scrobbleStatsForFile.ToString()));
                        }

                        scrobblerTagMarker.UpdateScrobblerStatsInTagLibFile(taglibFile, scrobbleStatsForFile);
                    }
                    catch (Exception e)
                    {
                        PrintError(String.Format("Error matching or updating scrobble tags for {0}: {1}",
                            scrobbleStatsForFile.ToString(), e.ToString()));
                    }
                }


                Console.WriteLine("Finished.");

            }
            catch (Exception e)
            {
                PrintError(e.ToString());
            }

            Console.ReadLine();
        }


        private static void PrintError(string message)
        {
            PrintMessage("Error: " + message);
        }

        private static void PrintMessage(string msg)
        {
            // Remove BELL characters (cause beep)
            msg = msg.Replace("•", "");

            Debug.WriteLine(msg);
            Console.Out.WriteLine(msg);
        }


        private static void LoadConfig()
        {
            PrintMessage("Reading config file...");
            string configText = System.IO.File.ReadAllText("settings.js");
            Config = JsonConvert.DeserializeObject<Settings>(configText);

        }

        public static string GetPathFromVolumeLabelAndRelativePath(string volumeLabel, string relativePath)
        {
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

                    if (driveInfo.VolumeLabel.ToLower() == volumeLabel.ToLower())
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
                throw new DriveNotFoundException("Could not find drive with volume label " + volumeLabel);
            }

            string resultPath = Path.Combine(driveDirectory.FullName, relativePath);
            return resultPath;
        }

    }
}
