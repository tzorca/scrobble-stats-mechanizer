using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrobbleStatsMechanizerCommon
{
    public class TagLibFileLoadResult
    {
        public TagLibFileLoadResult(string filePath)
        {
            FilePath = filePath;
        }

        public string FilePath { get; set; }
        public Exception Error { get; set; }
        public TagLib.File TagLibFile { get; set; }
    }
}
