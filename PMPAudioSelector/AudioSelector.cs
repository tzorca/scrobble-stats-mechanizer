using Newtonsoft.Json;
using System;
using ScrobbleStatsMechanizerCommon;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagLib;

namespace PMPAudioSelector
{
    public class AudioSelector
    {
        private const long BYTES_IN_MEGABYTE = 1024 * 1024;

        /// <summary>
        /// Load Tag Lib File for each file path in audioFilePaths
        /// </summary>
        /// <param name="audioFilePaths">The full paths to the audio files</param>
        /// <returns>A list of Tag Lib Files for the specified audio files</returns>
        public List<TagLib.File> GetTagLibFiles(List<string> audioFilePaths)
        {
            return audioFilePaths.Select(filePath => TagLib.File.Create(filePath)).ToList();
        }

        /// <summary>
        /// Copies sourceAudioFilePaths into destinationDirectoryPath, preserving file names. 
        /// </summary>
        /// <param name="sourceFilePaths">The full paths of the files to be copied</param>
        /// <param name="destinationDirectoryPath">The directory where the files will be copied to</param>
        /// <param name="messagePrintAction">The action to run when a status message needs to be printed</param>
        public void CopyFiles(List<string> sourceFilePaths, string destinationDirectoryPath, Action<string> messagePrintAction = null)
        {
            // Create all directories and subdirectories for the destination path unless they already exist.
            Directory.CreateDirectory(destinationDirectoryPath);

            // Copy files
            for (var index = 0; index < sourceFilePaths.Count; index++)
            {
                // Calculate percentage completion for copying
                var percentComplete = index / (double)sourceFilePaths.Count;

                var localFilePath = sourceFilePaths[index];
                string fileName = Path.GetFileName(localFilePath);
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(localFilePath);


                // Determine the full path of the file as it would be in the destination directory
                var destFilePath = Path.Combine(destinationDirectoryPath, fileName);

                if (System.IO.File.Exists(destFilePath))
                {
                    // The file already exists in the destination directory. No need to copy it.
                    if (messagePrintAction != null)
                    {
                        messagePrintAction(String.Format("{0:P2} | {1} already exists.", percentComplete, fileNameWithoutExt));
                    }
                    continue;
                }

                if (messagePrintAction != null)
                {
                    messagePrintAction(String.Format("{0:P2} | Copying {1}...", percentComplete, fileNameWithoutExt));
                }

                // Copy the source file to the destination directory
                System.IO.File.Copy(sourceFileName: localFilePath, destFileName: destFilePath);

            }
        }

        /// <summary>
        /// Given specified constraints, selects a list of audio files to be copied.
        /// </summary>
        /// <param name="tagLibFilesByTagTier">A list of prioritized tiers, where each tier has a list of TagLib files for that tier</param>
        /// <param name="destinationDrive">The drive where the files are to be copied</param>
        /// <param name="reservedMegabytes">The number of megabytes to leave free on the destination drive</param>
        /// <param name="maxFiles">The most files to copy at one time</param>
        /// <param name="messagePrintAction">The action to run when a status message needs to be printed</param>
        /// <returns>The resulting list of selected audio files</returns>
        public List<string> SelectAudioFilesUsingConstraints(List<List<TagLib.File>> tagLibFilesByTagTier, DriveInfo destinationDrive, long reservedMegabytes, int? maxFiles = null, Action<string> messagePrintAction = null)
        {
            var bytesAvailableAtDestination = destinationDrive.AvailableFreeSpace;

            var remainingBytes = bytesAvailableAtDestination - reservedMegabytes * BYTES_IN_MEGABYTE;
            if (remainingBytes < 0)
            {
                throw new IOException("Not enough space on destination drive to copy audio.");
            }

            var audioFilesToCopy = new List<string>();

            var rnd = new Random();
            int tierIdx = 0;

            while (remainingBytes > 0 || audioFilesToCopy.Count() >= maxFiles)
            {
                if (tagLibFilesByTagTier.Count <= tierIdx)
                {
                    // No more tiers or audio files left to try.
                    break;
                }

                var remainingTagLibFiles = tagLibFilesByTagTier[tierIdx];
                int numberOfTagFilesSelectedFromTier = 0;

                while (remainingTagLibFiles.Count > 0)
                {
                    var randomTagLibFile = remainingTagLibFiles.RemoveRandomElement(rnd);

                    var bytesInRandomAudioFile = new FileInfo(randomTagLibFile.Name).Length;

                    if (remainingBytes - bytesInRandomAudioFile < 0)
                    {
                        // This file would take up too many bytes. Break out of the loop.
                        break;
                    }

                    if (audioFilesToCopy.Count >= maxFiles)
                    {
                        // This file would make too many files. Break out of the loop.
                        break;
                    }

                    audioFilesToCopy.Add(randomTagLibFile.Name);
                    remainingBytes -= bytesInRandomAudioFile;
                    numberOfTagFilesSelectedFromTier += 1;
                }

                if (messagePrintAction != null)
                {
                    messagePrintAction(numberOfTagFilesSelectedFromTier + " audio files selected from tier " + (tierIdx + 1));
                }

                tierIdx++;
            }

            if (audioFilesToCopy.Count == 0)
            {
                throw new IOException("No files were selected to be copied.");
            }

            return audioFilesToCopy;
        }


        /// <summary>
        /// Groups audio files into multiple tiers based on specified TagLib tier conditions. When conditions overlap, tiers earlier in the list have precedence over tiers later in the list. Each audio file will be added to a
        /// </summary>
        /// <param name="tagLibAudioFiles">The TagLib audio files to group</param>
        /// <param name="tagLibTierConditions">The TagLib Tier conditions used for grouping.</param>
        /// <returns>A list of tiers, with each tier containing a list of qualifying audio files.</returns>
        public List<List<TagLib.File>> GroupTagLibFilesByTagTier(List<TagLib.File> tagLibAudioFiles, List<TagLibTierCondition> tagLibTierConditions)
        {
            var tagLibFilesByTagTier = new List<List<TagLib.File>>();

            var remainingFiles = new List<TagLib.File>(tagLibAudioFiles);

            for (int idx = 0; idx < tagLibTierConditions.Count; idx++)
            {
                var tagLibCondition = tagLibTierConditions[idx];
                tagLibFilesByTagTier.Add(remainingFiles.Where(tagLibCondition.Predicate).ToList());
                remainingFiles = remainingFiles.Where(tagLibCondition.Predicate.Not()).ToList();
            }

            return tagLibFilesByTagTier;
        }
    }
}
