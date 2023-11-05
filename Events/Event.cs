using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Budziszewski.Venture.Events
{
    public abstract class Event
    {
        public string UniqueId { get; protected set; } = "";

        public DateTime Timestamp { get; set; }

        public Assets.Asset ParentAsset { get; set; }

        public Event(Assets.Asset parentAsset)
        {
            ParentAsset = parentAsset;
        }


    }
}
