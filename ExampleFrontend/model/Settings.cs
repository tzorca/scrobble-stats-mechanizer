using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrobbleStatsMechanizer.ExampleFrontend
{
    /// <summary>
    /// For deserializing from a JSON file
    /// </summary>
    internal class Settings
    {
        /// <summary>
        /// The volume label to your PMP drive.
        /// Used with pmpScrobblerRelativeFilePath to determine the full path to the PMP scrobbler log file.
        /// </summary>
        public string pmpDriveVolumeLabel { get; set; }

        /// <summary>
        /// The relative path from the root of the PMP drive to the scrobbler log file.
        /// Used with pmpDriveVolumeLabel to determine the full path to the PMP scrobbler log file.
        /// In the example-settings.json, this will find drive with label of "SANSA CLIP", then the file in the root named ".scrobbler.log"
        /// </summary>
        public string pmpScrobblerRelativeFilePath { get; set; }

        /// <summary>
        /// Where to store the master scrobbler file.
        /// </summary>
        public string masterScrobblerFilePath { get; set; }

        /// <summary>
        /// Where to save backups of the master scrobbler file.
        /// </summary>
        public string scrobblerBackupsDirectoryPath { get; set; }

        /// <summary>
        /// Where your audio collection is stored.
        /// </summary>
        public string localAudioCollectionDirectoryPath { get; set; }

        /// <summary>
        /// TODO: Documentation
        /// </summary>
        public long pmpReservedMegabytes { get; set; }

        /// <summary>
        /// TODO: Documentation
        /// </summary>
        public string pmpAudioCollectionRelativePath { get; set; }

        /// <summary>
        /// For advanced users only. Not required. 
        /// After parsing the PMP scrobbler log file, this option determines whether to replace it with a zero-byte file.
        /// Used only to prevent duplicate entries from being added to the master scrobbler file on subsequent runs.
        /// </summary>
        public bool shouldDeletePMPScrobblerFile { get; set; }

        /// <summary>
        /// TODO: Documentation
        /// </summary>
        public int? pmpMaxAudioFilesToCopy { get; set; }
    }
}