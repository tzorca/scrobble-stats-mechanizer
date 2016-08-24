using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TagMarkerForScrobblers
{
    class Settings
    {
        public string VolumeLabel_Mp3Player { get; set; }
        public string RelativeFilePath_Mp3PlayerScrobbler { get; set; }
        public string DirectoryPath_ScrobblerBackups { get; set; }
        public string FilePath_MasterScrobbler { get; set; }
        public string DirectoryPath_AudioFiles { get; set; }
    }
}