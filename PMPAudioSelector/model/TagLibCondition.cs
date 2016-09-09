using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMPAudioSelector
{
    public class TagLibCondition
    {
        public TagLibCondition(Func<TagLib.File, bool> predicate)
        {
            this.Predicate = predicate;
        }

        public Func<TagLib.File, bool> Predicate { get; private set; }

    }
}
