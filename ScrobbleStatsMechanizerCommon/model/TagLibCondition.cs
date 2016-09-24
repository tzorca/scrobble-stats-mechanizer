using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrobbleStatsMechanizerCommon
{
    public class TagLibTierCondition
    {
        public TagLibTierCondition(Func<TagLib.File, bool> predicate)
        {
            this.Predicate = predicate;
        }

        public Func<TagLib.File, bool> Predicate { get; private set; }

    }
}
