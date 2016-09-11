using ScrobbleStatsMechanizerCommon;
using Newtonsoft.Json;
using PMPAudioSelector;
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
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                LoadSettings();

                if (args.Length == 0)
                {
                    // If no arguments provided, default to ScrobbleTagMarkerThenPMPAudioSelector mode
                    args = new string[] { ProgramMode.ScrobbleTagMarkerThenPMPAudioSelector.ToString() };
                }

                ProgramMode mode;
                if (!Enum.TryParse(args[0], out mode))
                {
                    // If an invalid mode is specified, list the mode options
                    var validProgramModes = Enum.GetNames(typeof(ProgramMode));
                    PrintError("Choose a program mode from the following list: " + string.Join(", ", validProgramModes));
                    return;
                }

                RunMode(mode);

                stopwatch.Stop();
                PrintMessage(String.Format("Finished in {0} seconds", stopwatch.Elapsed.TotalSeconds));
                Console.ReadLine();
            }
            catch (Exception e)
            {
                PrintError(e.ToString());
                Console.ReadLine();
            }
        }

        private static void RunMode(ProgramMode mode)
        {
            switch (mode)
            {
                case ProgramMode.ScrobbleTagMarker:
                    MarkScrobbleTags();
                    break;

                case ProgramMode.PMPAudioSelector:
                    SelectAndCopyAudioToPMP();
                    break;

                case ProgramMode.ScrobbleTagMarkerThenPMPAudioSelector:
                    MarkScrobbleTags();
                    SelectAndCopyAudioToPMP();
                    break;
            }
        }

        private static void MarkScrobbleTags()
        {
            var scrobblerTagMarker = new ScrobbleTagMarker();

            PrintMessage("Backing up current master scrobbler file...");
            scrobblerTagMarker.BackupScrobblerFile
            (
                masterScrobbleFilePath: Config.masterScrobblerFilePath,
                masterScrobbleBackupDirectoryPath: Config.scrobblerBackupsDirectoryPath
            );

            PrintMessage("Finding PMP scrobbler file path...");
            string mp3PlayerScrobbleFilePath = GetPathFromVolumeLabelAndRelativePath
            (
                volumeLabel: Config.pmpDriveVolumeLabel,
                relativePath: Config.pmpScrobblerRelativeFilePath
            );

            PrintMessage("Saving new scrobbler data from PMP...");
            scrobblerTagMarker.SaveNewScrobbleDataFromMP3Player
            (
                pmpScrobbleFilePath: mp3PlayerScrobbleFilePath,
                masterScrobbleFilePath: Config.masterScrobblerFilePath,
                truncateScrobblerFile: Config.shouldDeletePMPScrobblerFile
            );


            PrintMessage("Loading tags from audio files...");
            var tagLibFileLoadResults = scrobblerTagMarker.GetTagLibFiles(audioCollectionDirectoryPath: Config.localAudioCollectionDirectoryPath);
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
            var scrobblerData = scrobblerTagMarker.ParseScrobblerData(Config.masterScrobblerFilePath);

            PrintMessage("Aggregating scrobbler data...");
            var scrobblerStatsForFiles = scrobblerTagMarker.AggregateScrobblerData(scrobblerData);

            PrintMessage("Matching scrobbler stats with files and updating tag stats...");

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
        }


        private static void SelectAndCopyAudioToPMP()
        {
            var audioSelector = new AudioSelector();


            PrintMessage("Retrieving list of local audio files...");
            var localAudioFilePaths = new List<string>(Directory.GetFiles(Config.localAudioCollectionDirectoryPath));

            PrintMessage(String.Format("Finding PMP drive by volume name ({0})...", Config.pmpDriveVolumeLabel));
            var pmpDrive = GetDriveFromVolumeLabel(Config.pmpDriveVolumeLabel);


            PrintMessage("Loading audio file tags...");
            var tagLibFiles = audioSelector.GetTagLibFiles(localAudioFilePaths);

            PrintMessage("Grouping audio files by tag tier...");
            var tagLibFilesByTagTier = audioSelector.GroupTagLibFilesByTagTier(tagLibFiles, BuildExampleTagLibConditions());

            PrintMessage("Selecting audio files to copy...");
            var selectedAudioFilePaths = audioSelector.SelectAudioFilesToCopy(tagLibFilesByTagTier, pmpDrive, Config.pmpReservedMegabytes, PrintMessage);

            PrintMessage("Copying selected audio files to PMP...");

            // Find PMP audio directory
            var pmpAudioDirectoryPath = Path.Combine(pmpDrive.RootDirectory.FullName, Config.pmpAudioCollectionRelativePath);

            audioSelector.CopyAudioFilesToPMP(selectedAudioFilePaths, pmpAudioDirectoryPath, PrintMessage);

        }

        /// <summary>
        /// TODO: Implement defining these in settings file instead.
        /// </summary>
        /// <returns></returns>
        private static List<TagLibCondition> BuildExampleTagLibConditions()
        {
            var tagLibTiers = new List<TagLibCondition>();

            // Tier 1: Condition 1: Audio files that haven't been played before
            // Tier 1: Condition 2: Audio files that have been played 1 to 3 times, have a good rating, and haven't been played for 30 days
            tagLibTiers.Add(new TagLibCondition(file =>
                file.GetTimesPlayed() < 1 ||
                (
                    file.GetTimesPlayed() >= 1 && file.GetTimesPlayed() <= 3 &&
                    file.RatingIsGoodOrBetter() &&
                    file.GetLastPlayed().HasValue && file.DaysSinceLastPlayed() >= 30
                )
            ));

            // Tier 2: Audio files with a good rating that hasn't been played for 30 days
            tagLibTiers.Add(new TagLibCondition(file =>
                file.RatingIsGoodOrBetter() &&
                file.GetLastPlayed().HasValue && file.DaysSinceLastPlayed() > 30
            ));


            // Tier 3: Audio files with a good rating
            tagLibTiers.Add(new TagLibCondition(file =>
                file.RatingIsGoodOrBetter()
            ));

            // Tier 4: Everything else
            tagLibTiers.Add(new TagLibCondition(file => true));

            return tagLibTiers;
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


        private static void LoadSettings()
        {
            PrintMessage("Reading settings file...");
            string configText = System.IO.File.ReadAllText("settings.json");
            Config = JsonConvert.DeserializeObject<Settings>(configText);
        }


        private static DriveInfo GetDriveFromVolumeLabel(string volumeLabel)
        {
            var driveInfoList = DriveInfo.GetDrives();
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
                        return driveInfo;
                    }
                }
                catch (Exception e)
                {
                    PrintError(e.Message);
                }
            }

            throw new DriveNotFoundException("Could not find drive with volume label " + volumeLabel);

        }

        private static string GetPathFromVolumeLabelAndRelativePath(string volumeLabel, string relativePath)
        {
            DriveInfo drive = GetDriveFromVolumeLabel(volumeLabel);
            return Path.Combine(drive.RootDirectory.FullName, relativePath);
        }

    }
}
