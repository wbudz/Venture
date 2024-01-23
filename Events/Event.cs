using Venture.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture.Events
{
    public enum PaymentDirection { Unspecified, Neutral, Inflow, Outflow }

    public abstract class Event
    {
        public string UniqueId { get; protected set; } = $"Event_{Guid.NewGuid()}";

        public int TransactionIndex { get; protected set; } = -1;

        public DateTime Timestamp { get; set; }

        public Assets.Asset ParentAsset { get; set; }

        public decimal Amount { get; set; } = 0;

        public decimal FXRate { get; protected set; } = 1;

        public Event(Assets.Asset parentAsset, DateTime timestamp)
        {
            ParentAsset = parentAsset;
            Timestamp = timestamp;
        }

        public override string ToString()
        {
            return UniqueId;
        }
    }
}
