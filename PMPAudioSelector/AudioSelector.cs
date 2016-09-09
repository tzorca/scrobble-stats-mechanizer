using Newtonsoft.Json;
using System;
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

        public List<TagLib.File> GetTagLibFiles(List<string> localAudioFilePaths)
        {
            return localAudioFilePaths.Select(filePath => TagLib.File.Create(filePath)).ToList();
        }

        public void CopyAudioFilesToPMP(List<string> selectedAudioFiles, string pmpAudioDirectoryPath, Action<string> messagePrintAction = null)
        {


            // Create all directories and subdirectories for the PMP audio directory path unless they already exist.
            Directory.CreateDirectory(pmpAudioDirectoryPath);

            // Copy files
            for (var index = 0; index < selectedAudioFiles.Count; index++)
            {
                // Calculate percentage completion for copying
                var percentComplete = index / (double)selectedAudioFiles.Count;

                var localFilePath = selectedAudioFiles[index];
                string fileName = Path.GetFileName(localFilePath);
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(localFilePath);


                // Determine the full path of the file as it would be on the PMP
                var pmpFilePath = Path.Combine(pmpAudioDirectoryPath, fileName);

                if (System.IO.File.Exists(pmpFilePath))
                {
                    // The file already exists in the PMP audio directory. No need to copy it.
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

                // Copy the file from local to PMP
                System.IO.File.Copy(sourceFileName: localFilePath, destFileName: pmpFilePath);

            }
        }


        private const long BYTES_IN_MEGABYTE = 1024 * 1024;
        public List<string> SelectAudioFilesToCopy(List<List<TagLib.File>> tagLibFilesByTagTier, DriveInfo pmpDrive, long reservedMegabytes, Action<string> messagePrintAction = null)
        {
            var bytesAvailableOnPMP = pmpDrive.AvailableFreeSpace;

            var remainingBytes = bytesAvailableOnPMP - reservedMegabytes * BYTES_IN_MEGABYTE;
            if (remainingBytes < 0)
            {
                throw new IOException("Not enough space on PMP to copy audio.");
            }

            var audioFilesToCopy = new List<string>();

            var rnd = new Random();
            int tierIdx = 0;

            while (remainingBytes > 0)
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
                throw new IOException("There wasn't enough space to copy any files.");
            }

            return audioFilesToCopy;
        }



        public List<List<TagLib.File>> GroupTagLibFilesByTagTier(List<TagLib.File> tagLibAudioFiles, List<TagLibCondition> tagLibConditions)
        {
            var tagLibFilesByTagTier = new List<List<TagLib.File>>();

            var remainingFiles = new List<TagLib.File>(tagLibAudioFiles);

            for (int idx = 0; idx < tagLibConditions.Count; idx++)
            {
                var tagLibCondition = tagLibConditions[idx];
                tagLibFilesByTagTier.Add(remainingFiles.Where(tagLibCondition.Predicate).ToList());
                remainingFiles = remainingFiles.Where(tagLibCondition.Predicate.Not()).ToList();
            }

            return tagLibFilesByTagTier;
        }


    }
}
