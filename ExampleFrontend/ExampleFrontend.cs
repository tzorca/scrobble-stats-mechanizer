using Newtonsoft.Json;
using PMPAudioSelector;
using ScrobbleStatsMechanizerCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrobbleStatsMechanizer.ExampleFrontend
{
    public class ExampleFrontend
    {
        private Settings Config { get; set; }

        public void LoadSettings()
        {
            PrintMessage("Reading settings file...");
            string configText = System.IO.File.ReadAllText("settings.json");
            Config = JsonConvert.DeserializeObject<Settings>(configText);
        }

        public void RunMode(FrontendMode mode)
        {
            switch (mode)
            {
                case FrontendMode.ScrobbleTagMarker:
                    MarkScrobbleTags();
                    break;

                case FrontendMode.PMPAudioSelector:
                    SelectAndCopyAudioToPMP();
                    break;

                case FrontendMode.ScrobbleTagMarkerThenPMPAudioSelector:
                    MarkScrobbleTags();
                    SelectAndCopyAudioToPMP();
                    break;

                default:
                    break;
            }
        }

        public FrontendMode DetermineMode(string[] args)
        {

            if (args.Length == 0)
            {
                // If no arguments provided, default to ScrobbleTagMarkerThenPMPAudioSelector mode
                args = new string[] { FrontendMode.ScrobbleTagMarkerThenPMPAudioSelector.ToString() };
            }

            FrontendMode mode;
            if (Enum.TryParse(args[0], out mode))
            {
                return mode;
            }
            else
            {
                // If an invalid mode is specified, list the mode options
                var validProgramModes = Enum.GetNames(typeof(FrontendMode));
                PrintError("Choose a program mode from the following list: " + string.Join(", ", validProgramModes));
                return FrontendMode.Unknown;
            }
        }

        public void PrintMessage(string msg)
        {
            // Remove BELL characters (cause beep)
            msg = msg.Replace("•", "");

            Debug.WriteLine(msg);
            Console.Out.WriteLine(msg);
        }


        public void PrintError(string message)
        {
            PrintMessage("Error: " + message);
        }

        public DriveInfo GetDriveFromVolumeLabel(string volumeLabel)
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

        internal string GetPathFromVolumeLabelAndRelativePath(string volumeLabel, string relativePath)
        {
            DriveInfo drive = GetDriveFromVolumeLabel(volumeLabel);
            return Path.Combine(drive.RootDirectory.FullName, relativePath);
        }


        internal void MarkScrobbleTags()
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


        internal void SelectAndCopyAudioToPMP()
        {
            var audioSelector = new AudioSelector();


            PrintMessage("Retrieving list of local audio files...");
            var localAudioFilePaths = new List<string>(Directory.GetFiles(Config.localAudioCollectionDirectoryPath));

            PrintMessage(String.Format("Finding PMP drive by volume name ({0})...", Config.pmpDriveVolumeLabel));
            var pmpDrive = GetDriveFromVolumeLabel(Config.pmpDriveVolumeLabel);

            PrintMessage("Loading audio file tags...");
            var tagLibFiles = audioSelector.GetTagLibFiles(localAudioFilePaths);

            PrintMessage("Grouping audio files by tag tier...");
            var tagLibFilesByTagTier = audioSelector.GroupTagLibFilesByTagTier(tagLibFiles, BuildExampleTagLibTierConditions());

            PrintMessage("Selecting audio files to copy...");
            var selectedAudioFilePaths = audioSelector.SelectAudioFilesUsingConstraints(tagLibFilesByTagTier, pmpDrive, Config.pmpReservedMegabytes, Config.pmpMaxAudioFilesToCopy, PrintMessage);

            PrintMessage("Copying selected audio files to PMP...");

            // Find PMP audio directory
            var pmpAudioDirectoryPath = Path.Combine(pmpDrive.RootDirectory.FullName, Config.pmpAudioCollectionRelativePath);

            audioSelector.CopyFiles(selectedAudioFilePaths, pmpAudioDirectoryPath, PrintMessage);

        }

        /// <summary>
        /// An example function for creating tiers of audio selection conditions.
        /// </summary>
        /// <returns></returns>
        public virtual List<TagLibTierCondition> BuildExampleTagLibTierConditions()
        {
            var tagLibTiers = new List<TagLibTierCondition>();

            // Tier 1: Condition 1: Audio files that haven't been played before
            // Tier 1: Condition 2: Audio files that have been played 1 to 3 times, have a good rating, and haven't been played for 30 days
            tagLibTiers.Add(new TagLibTierCondition(file =>
                file.GetTimesPlayed() < 1 ||
                (
                    file.GetTimesPlayed() >= 1 && file.GetTimesPlayed() <= 3 &&
                    file.RatingIsGoodOrBetter() &&
                    file.GetLastPlayed().HasValue && file.DaysSinceLastPlayed() >= 30
                )
            ));

            // Tier 2: Audio files with a good rating that haven't been played for 30 days
            tagLibTiers.Add(new TagLibTierCondition(file =>
                file.RatingIsGoodOrBetter() &&
                file.GetLastPlayed().HasValue && file.DaysSinceLastPlayed() > 30
            ));


            // Tier 3: Audio files with a good rating
            tagLibTiers.Add(new TagLibTierCondition(file =>
                file.RatingIsGoodOrBetter()
            ));

            // Tier 4: Everything else
            tagLibTiers.Add(new TagLibTierCondition(file => true));

            return tagLibTiers;
        }

    }
}
